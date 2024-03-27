using System;
using System.IO;
using static WiseUnpacker.HWUN.HexaDeci;

namespace WiseUnpacker.HWUN
{
    internal class HWUN
    {
        // State
        private MultipartFile? _inputFile;
        private Stream? _dumpFile;
        private bool _pkzip;
        private bool _realfound;

        // Options
        private uint _rollback;
        private uint _userOffset;
        private bool _renaming;

        #region Read-Only Multifile Section

        public unsafe class BufferedFile
        {
            public Stream? FileHandle { get; private set; }
            public string? FileName { get; private set; }
            public byte[] Buffer { get; private set; } = new byte[0x8000]; // [$0000..$7fff]
            public uint Error { get; private set; }
            public uint bo { get; private set; }
            public uint fs { get; private set; }
            public uint FilePosition { get; private set; }

            public void Open(string s)
            {
                FileHandle = File.OpenRead(s);
                Buffer = new byte[0x8000];
                // b = &bf.bfilebuf[0]
                fs = (uint)FileHandle.Length;
                bo = 0xffff0000;
                FilePosition = 0x00000000;
            }

            public void Close()
            {
                Buffer = [];
                FileHandle?.Close();
            }

            public bool Valid(uint p)
            {
                return p >= 0 && p < fs;
            }

            public bool InMem(uint p, uint l)
            {
                return (p >= bo) && (p + 1 <= bo + 0x8000);
            }

            public bool EOF()
            {
                return FilePosition == fs;
            }

            public void FillBuffer(uint p)
            {
                bo = p - 0x4000;
                if (bo < 0x0000)
                    bo = 0x0000;
                if (bo + 0x8000 > fs)
                    bo = fs - 0x8000;

                // filesize <$8000
                if (bo < 0)
                {
                    bo = 0;
                    FileHandle!.Seek(bo, SeekOrigin.Begin);
                    FileHandle.Read(Buffer!, 0, (int)fs);
                }
                else
                {
                    FileHandle!.Seek(bo, SeekOrigin.Begin);
                    FileHandle.Read(Buffer!, 0, 0x8000);
                }
                FilePosition = p;
            }

            public byte ReadByte()
                => ReadByte(FilePosition);

            public byte ReadByte(uint p)
            {
                byte res = 0;
                if (Valid(p))
                {
                    if (!InMem(p, 1))
                        FillBuffer(p);

                    res = Buffer![FilePosition - bo];
                    FilePosition++;
                }
                else
                {
                    Error = 1;
                }

                return res;
            }

            public ushort ReadWord()
                => ReadWord(FilePosition);

            public ushort ReadWord(uint p)
            {
                ushort res = 0;
                if (Valid(p))
                {
                    if (!InMem(p, 2))
                        FillBuffer(p);

                    res = BitConverter.ToUInt16(Buffer!, (int)(p - bo));
                    FilePosition++;
                }
                else
                {
                    Error = 1;
                }

                return res;
            }

            public uint ReadLongInt()
                => ReadLongInt(FilePosition);

            public uint ReadLongInt(uint p)
            {
                uint res = 0;
                if (Valid(p))
                {
                    if (!InMem(p, 4))
                        FillBuffer(p);

                    res = BitConverter.ToUInt32(Buffer!, (int)(p - bo));
                    FilePosition++;
                }
                else
                {
                    Error = 1;
                }

                return res;
            }

            public void Seek(uint p)
            {
                FilePosition = p;
            }
        }

        public class MultipartFile
        {
            public Stream? FileHandle { get; private set; }
            public uint fs { get; private set; }
            public uint FilePosition { get; private set; }
            public uint FileEnd { get; private set; }
            public uint FileLength { get; private set; }
            public MultipartFile? Next { get; private set; }
            // public MultipartFileBuffer? FileBuffer { get; private set; }

            public static bool Open(string name, out MultipartFile? mf)
            {
                try
                {
                    mf = new MultipartFile();

                    mf.FileHandle = File.OpenRead(name);
                    mf.fs = 0;
                    mf.FilePosition = 0;
                    mf.FileEnd = (uint)(mf.FileHandle.Length - 1);
                    mf.FileLength = mf.FileEnd + 1;
                    mf.Next = null;

                    return true;
                }
                catch
                {
                    mf = null;
                    return false;
                }
            }

            public bool Append(string name)
            {
                MultipartFile mf = this;
                while (mf.Next != null)
                {
                    mf = mf.Next;
                }

                if (Open(name, out MultipartFile? next))
                {
                    mf.Next = next;
                    mf.Next!.fs = FileLength;
                    mf.Next.FileEnd += FileLength;
                    FileLength = mf.Next.FileEnd + 1;
                    return true;
                }
                else
                {
                    mf.Next = null;
                    return false;
                }
            }

            public void Seek(uint pos)
            {
                FilePosition = pos;
            }

            public void Close()
            {
                Next?.Close();
                FileHandle?.Close();
            }

            public bool BlockRead(byte[] buffer, ushort amount)
            {
                MultipartFile mf = this;
                while (FilePosition > mf.FileEnd && mf.Next != null)
                {
                    mf = mf.Next;
                }

                if (FilePosition > mf.FileEnd)
                    return false;

                mf.FileHandle!.Seek(FilePosition - mf.fs, SeekOrigin.Begin);
                if (mf.FileEnd + 1 - FilePosition > amount)
                {
                    mf.FileHandle.Read(buffer, 0, amount);
                    FilePosition += amount;
                }
                else
                {
                    byte[] buf = new byte[0xffff];
                    uint bufpos = 0;
                    do
                    {
                        if (mf!.FileEnd + 1 < FilePosition + amount - bufpos)
                        {
                            mf.FileHandle!.Read(buf, (int)bufpos, (int)(mf.FileEnd + 1 - FilePosition));
                            bufpos += mf.FileEnd + 1 - FilePosition;
                            FilePosition = mf.FileEnd + 1;
                            mf = mf.Next!;
                        }
                        else
                        {
                            mf.FileHandle!.Read(buf, (int)bufpos, (int)(amount - bufpos));
                            FilePosition += amount - bufpos;
                            bufpos = amount;
                        }
                    } while (bufpos != amount);

                    Array.Copy(buf, buffer, amount);
                }

                return true;
            }
        }

        // public class MultipartFileBuffer
        // {
        //     public byte[] Buffer = new byte[0x8000];
        //     public ushort bp;
        //     public uint bs;
        //     public uint be;
        // }

        #endregion

        #region Inflate Read/Write section

        public InflateImpl invflatev = new();
        public class InflateImpl : SINFLATE
        {
            public MultipartFile? Input { get; private set; }
            public Stream? Output { get; private set; }
            public byte[] InputBuffer { get; private set; } = new byte[0x4000];
            public ushort InputBufferPosition { get; private set; }
            public ushort InputBufferSize { get; private set; }
            public uint InputSize { get; private set; }
            public uint OutputSize { get; private set; }
            public ushort Result { get; private set; }
            public uint CRC { get; set; }

            public void Inflate(MultipartFile inf, string outf)
            {
                InputBuffer = new byte[0x4000];
                Input = inf;
                Output = File.OpenWrite(outf);
                InputSize = 0;
                OutputSize = 0;
                InputBufferSize = (ushort)InputBuffer.Length;
                InputBufferPosition = InputBufferSize;

                CRC = CRC32.Start();
                SI_INFLATE();
                inf.Seek(inf.FilePosition - InputBufferSize + InputBufferPosition);
                CRC = CRC32.End(CRC);

                Result = SI_ERROR;
                Output.Close();
                InputBuffer = [];
            }

            public override byte SI_READ()
            {
                if (InputBufferPosition >= InputBufferSize)
                {
                    if (Input!.FilePosition == Input!.FileLength)
                    {
                        SI_BREAK = true;
                    }
                    else
                    {
                        if (InputBufferSize > Input!.FileLength - Input!.FilePosition)
                            InputBufferSize = (ushort)(Input!.FileLength - Input!.FilePosition);

                        Input!.BlockRead(InputBuffer, InputBufferSize);
                        InputBufferPosition = 0x0000;
                    }
                }

                byte inflateread = InputBuffer[InputBufferPosition];
                InputSize++;
                InputBufferPosition++;
                return inflateread;
            }

            public override void SI_WRITE(ushort amount)
            {
                OutputSize += amount;
                Output!.Write(SI_WINDOW, 0, amount);
                CRC = CRC32.Add(CRC, SI_WINDOW, amount);
            }
        }

        public Screen scrnv = new();
        public class Screen
        {
            public uint offsa;
            public uint offsr;
            public uint filestart;
            public uint fileend;
            public uint extract;
        }

        private void writestatus(string s)
        {
            Console.Write(s);
        }

        #endregion

        #region HWUN main section

        private string? olddir;

        private bool OpenFile(string name)
        {
            string fn = name;
            bool bo = MultipartFile.Open(Path.Combine(olddir!, fn), out _inputFile);
            if (bo)
            {
                if (fn[fn.Length - 3] == '.')
                    fn = fn.Substring(0, fn.Length - 4);
            }
            else
            {
                bo = MultipartFile.Open(Path.Combine(olddir!, $"{name}.exe"), out _inputFile);
            }

            if (bo)
            {
                writestatus("Installation is made of 01 file");
                byte fileno = 2;
                while (_inputFile!.Append(olddir + Path.DirectorySeparatorChar + fn + ".w" + (char)(fileno / 10 + 48) + (char)(fileno % 10 + 48)))
                {
                    writestatus("Installation is made of " + (char)(fileno / 10 + 48) + (char)(fileno % 10 + 48) + " files");
                    fileno++;
                }
            }
            else
            {
                writestatus("ERROR: File could not be opened");
            }

            return bo;
        }

        private void CloseFile()
        {
            _inputFile!.Close();
        }

        private void Approximate()
        {
            uint l0, l1;

            byte[] buf = new byte[0xc200];

            writestatus("Approximating to archive offset");

            _inputFile!.Seek(0x0000);
            _inputFile!.BlockRead(buf, 0xc000);
            scrnv.offsr = 0xbffc;

            uint l2 = 0;
            while (((buf[scrnv.offsa] != 0x00) || (buf[scrnv.offsa + 1] != 0x00)) && scrnv.offsa > 0x20 && l2 != 1)
            {
                scrnv.offsa--;
                if (buf[scrnv.offsa] == 0x00 && buf[scrnv.offsa + 1] == 0x00)
                {
                    l1 = 0;
                    for (l0 = 0x01; l0 <= 0x20; l0++)
                    {
                        if (buf[scrnv.offsa - l0] == 0x00)
                            l1++;
                    }
                    if (l1 < 0x04)
                        scrnv.offsa -= 2;
                }
            }

            scrnv.offsa += 2;

            while (buf[scrnv.offsa + 3] == 0x00 && scrnv.offsa + 4 < 0xc000)
            {
                scrnv.offsa += 4;
            }

            if (buf[scrnv.offsa] <= 0x20 && buf[scrnv.offsa + 1] > 0x00 && buf[scrnv.offsa + 1] + scrnv.offsa + 3 < 0xc000)
            {
                l1 = (uint)(buf[scrnv.offsa + 1] + 0x02);
                l2 = 0x00;
                for (l0 = 0x02; l0 <= l1 - 0x01; l0++)
                {
                    if (buf[scrnv.offsa + l0] >= 0x80)
                        l2++;
                }

                if (l2 * 0x100 / l1 < 0x10)
                    scrnv.offsa += l1;
            }

            l0 = 0x02;
            while (l2 != 0x04034b50 && l0 < 0x80 && scrnv.offsa - l0 >= 0 && scrnv.offsa - l0 <= 0xbffc)
            {
                l2 = BitConverter.ToUInt32(buf, (int)(scrnv.offsa - l0));
                l0++;
            }

            if (l0 < 0x80)
            {
                _pkzip = true;
                l0 = 0x0000;
                l1 = 0x0000;
                while (l1 != 0x04034b50 && l0 < scrnv.offsa)
                {
                    l1 = BitConverter.ToUInt32(buf, (int)l0);
                    l0++;
                }

                l0--;
                scrnv.offsa = l0;
                if (l1 != 0x04034b50)
                    _pkzip = false;
            }
            else
            {
                _pkzip = false;
            }
        }

        private void FindReal()
        {
            byte[] newcrcbytes = new byte[4];
            uint newcrc = 0, pos;

            if (!_pkzip)
            {
                if (scrnv.offsa < 0x100)
                    scrnv.offsa = 0x100;
                else if (scrnv.offsa >= 0xbf00)
                    scrnv.offsa = 0xbf00;
            }

            if (scrnv.offsa >= 0x0000 && scrnv.offsa <= 0xffff)
            {
                writestatus("Detecting real archive offset");
                pos = 0x0000;
                do
                {
                    _inputFile!.Seek(scrnv.offsa + pos);
                    invflatev.Inflate(_inputFile!, "WISE0001");
                    _inputFile!.BlockRead(newcrcbytes, 4);
                    newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                    scrnv.offsr = scrnv.offsa + pos;
                    pos++;
                } while ((invflatev.CRC != newcrc || invflatev.Result != 0x0000 || newcrc == 0x00000000) && pos != 0x100);

                if ((invflatev.CRC != newcrc || newcrc == 0x00000000 || invflatev.Result != 0x0000) && pos == 0x100)
                {
                    unchecked
                    {
                        pos = (uint)-1;
                        do
                        {
                            _inputFile!.Seek(scrnv.offsa + pos);
                            invflatev.Inflate(_inputFile!, "WISE0001");
                            _inputFile!.BlockRead(newcrcbytes, 4);
                            newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                            scrnv.offsr = scrnv.offsa + pos;
                            pos--;
                        } while ((invflatev.CRC != newcrc || invflatev.Result != 0x0000 || newcrc == 0x00000000) && pos != (uint)-0x100);
                    }
                }
            }
            else
            {
                invflatev.CRC = ~newcrc;
                unchecked { pos = (uint)-0x100; }
            }

            unchecked
            {
                if ((invflatev.CRC != newcrc || newcrc == 0x00000000 || invflatev.Result != 0x0000) && pos == (uint)-0x100)
                {
                    writestatus("ERROR: The file doesn''t seem to be a WISE installation");
                    _realfound = false;
                    scrnv.offsr = 0xffffffff;
                }
                else
                {
                    _realfound = true;
                }
            }
        }

        private void ExtractFiles()
        {
            uint newcrc = 0;
            byte[] newcrcbytes = new byte[4];
            uint attempt, fs;
            byte[] buf = new byte[0x400];
            ushort len1, len2;
            byte[] len1bytes = new byte[2], len2bytes = new byte[2];

            writestatus("Extracting files");
            _dumpFile = File.OpenWrite("WISE0000");
            _inputFile!.Seek(scrnv.offsr);
            do
            {
                scrnv.extract++;
                fs = _inputFile!.FilePosition;
                if (_pkzip)
                {
                    _inputFile!.BlockRead(buf, 0xe);
                    _inputFile!.BlockRead(newcrcbytes, 0x04);
                    newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                    _inputFile!.BlockRead(buf, 0x08);
                    _inputFile!.BlockRead(len1bytes, 0x02);
                    len1 = BitConverter.ToUInt16(len1bytes, 0);
                    _inputFile!.BlockRead(len2bytes, 0x02);
                    len2 = BitConverter.ToUInt16(len2bytes, 0);
                    if (len1 + len2 > 0)
                        _inputFile!.BlockRead(buf, (ushort)(len1 + len2));
                }

                invflatev.Inflate(_inputFile!, "WISE" + Hexa(BitConverter.GetBytes(scrnv.extract), 4));
                scrnv.filestart = fs;

                if (_pkzip)
                    invflatev.CRC = 0x04034b50;

                scrnv.fileend = scrnv.filestart + invflatev.InputSize - 1;
                if (invflatev.Result == 0x0000)
                {
                    _inputFile!.BlockRead(newcrcbytes, 4);
                    newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                    attempt = 0;
                    while (invflatev.CRC != newcrc && attempt < 8 && _inputFile!.FilePosition + 1 < _inputFile!.FileLength)
                    {
                        _inputFile!.Seek(_inputFile!.FilePosition - 3);
                        _inputFile!.BlockRead(newcrcbytes, 4);
                        newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                        attempt++;
                    }

                    scrnv.fileend = _inputFile!.FilePosition - 1;
                    if (_pkzip)
                    {
                        scrnv.fileend -= 4;
                        _inputFile!.Seek(_inputFile!.FilePosition - 4);
                    }
                }

                if (invflatev.Result != 0x0000 || newcrc != invflatev.CRC)
                {
                    invflatev.CRC = 0xffffffff;
                    newcrc = 0xfffffffe;
                }

                _dumpFile.Write(BitConverter.GetBytes(scrnv.filestart), 0, 4);
            } while (newcrc != invflatev.CRC);

            _dumpFile.Write(BitConverter.GetBytes(scrnv.fileend), 0, 4);
            _dumpFile.Close();
        }

        private void RenameFiles()
        {
            BufferedFile bf = new BufferedFile();
            BufferedFile df = new BufferedFile();
            string nn = string.Empty;
            uint fileno;
            uint sh0 = 0, sh1 = 0, offs, l, l0, l1 = 0, l2, l3 = 0, l4, l5, res;
            uint instcnt;
            Stream f;

            writestatus("Searching for script file");
            fileno = 0;
            res = 1;
            instcnt = 0;
            while (fileno < scrnv.extract && fileno < 6 && res != 0)
            {
                fileno++;
                bf.Open("WISE" + Hexa(BitConverter.GetBytes(fileno), 4));
                l = 0x0000;
                while (res != 0 && l < bf.fs)
                {
                    while (l < bf.fs && (bf.ReadByte(l + 0) != 0x25) || bf.ReadByte(l + 1) != 0x5c)
                        l++;
                }
                if (l < bf.fs)
                {
                    l1 = 0x01;
                    while (l1 < 0x40 && (bf.ReadByte(l - l1 + 0) != 0x25 || bf.ReadByte(l - l1 + 1) == 0x5c))
                        l1++;
                    if (l1 < 0x40)
                        res = 0;
                    else
                        l++;
                }

                if (res != 0)
                    bf.Close();
            }

            if (fileno < 6 && fileno < scrnv.extract)
            {
                writestatus("Calculating offset shift value");
                df.Open("WISE0000");
                l5 = (df.fs - 0x04) / 0x04;

                do
                {
                    do
                    {
                        l1 = df.ReadLongInt(l5 * 0x04 - 0x04);
                        l2 = df.ReadLongInt(l5 * 0x04 - 0x00);
                        l = bf.fs - 0x07;
                        res = 1;
                        while (l >= 0 && res != 0)
                        {
                            l--;
                            l3 = bf.ReadLongInt(l + 0x00);
                            l4 = bf.ReadLongInt(l + 0x04);
                            if (l4 > l3 && l4 < l2 && l3 < l1 && l4 - l3 == l2 - l1)
                                res = 0;
                        }

                        if (res != 0)
                            l5--;
                    } while (res != 0 && l5 != 0);
                    sh0 = l1 - l3;

                    if (res == 0)
                    {
                        do
                        {
                            l1 = df.ReadLongInt(l5 * 0x04 - 0x04);
                            l2 = df.ReadLongInt(l5 * 0x04 - 0x00);
                            l = bf.fs - 0x07;
                            l = 1;
                            while (l >= 0 && res != 0)
                            {
                                l--;
                                l3 = bf.ReadLongInt(l + 0x00);
                                l4 = bf.ReadLongInt(l + 0x04);
                                if (l4 > l3 && l4 < l2 && l3 < l1 && l4 - l3 == l2 - l1)
                                    res = 0;
                            }

                            if (res != 0)
                                l5--;
                        } while (res != 0 && l5 != 0);
                        sh1 = l1 - l3;
                    }

                } while (l5 != 0 && (res != 0 || sh0 != sh1));

                if (res == 0)
                {
                    // shiftvalue = sh0
                    writestatus("Renaming files");
                    l5 = 0x04;
                    while (l5 + 8 < df.fs)
                    {
                        l5 += 0x04;
                        l1 = df.ReadLongInt(l5 + 0x00);
                        l2 = df.ReadLongInt(l5 + 0x04);
                        l0 = 0xffffffff;
                        res = 1;
                        while (l0 + 0x29 < bf.fs && res != 0)
                        {
                            l0++;
                            l3 = bf.ReadLongInt(l0 + 0x00);
                            l4 = bf.ReadLongInt(l0 + 0x04);
                            if ((l1 == l + sh0) && (l2 == l4 + sh0))
                                res = 0;
                        }

                        if (res == 0)
                        {
                            l2 = bf.ReadWord(l0 - 2);
                            nn = "";
                            offs = l0;
                            l0 += 0x28;
                            res = 2;
                            if (bf.ReadByte(l0) == 0x25)
                            {
                                while (bf.ReadByte(l0) != 0)
                                {
                                    nn = nn + (char)bf.ReadByte(l0);
                                    if (bf.ReadByte(l0) < 0x20)
                                        res = 1;
                                    if (bf.ReadByte(l0) == 0x25 && res != 1)
                                        res = 3;
                                    if (bf.ReadByte(l0) == 0x5c && (bf.ReadByte(l0 - 1) == 0x25) && res == 3)
                                        res = 4;
                                    if (res == 4)
                                        res = 0;
                                    l0++;
                                }
                            }
                            if (res != 0)
                                res = 0x80;
                        }

                        l1 = (l5 + 0x04) / 0x04;
                        if (res == 0)
                        {
                            l0 = l;
                            while (l0 < nn.Length)
                            {
                                if (nn[(int)l0] == '%')
                                    nn = nn.Substring(1, (int)(l0 - 1)) + nn.Substring((int)(l0 + 1), (int)(nn.Length - l0));
                                else if (nn[(int)l0] == '\\' && nn[(int)(l0 - 1)] == '\\')
                                    nn = nn.Substring(1, (int)(l0 - 1)) + nn.Substring((int)(l0 + 1), (int)(nn.Length - l0));
                                else
                                    l0++;
                            }
                            f = File.OpenWrite("WISE" + Hexa(BitConverter.GetBytes(l1), 4));

                            // Make directories
                            l0 = 0;
                            while (l0 < nn.Length)
                            {
                                l0++;
                                if (nn[(int)l0] == '\\')
                                {
                                    Directory.CreateDirectory(nn.Substring(1, (int)(l0 - 1)));
                                }
                            }

                            // Rename file
                            do
                            {
                                var tempout = File.OpenWrite(nn);
#if NET20 || NET35
                                byte[] tempbytes = new byte[f.Length];
                                f.Read(tempbytes, 0, tempbytes.Length);
                                tempout.Write(tempbytes, 0, tempbytes.Length);
#else
                                f.CopyTo(tempout);
#endif
                                f.Close();

                                l2 = (uint)nn.Length;
                                while (nn[(int)l2] != '.' && l2 > 0x00)
                                    l2--;

                                if (l2 == 0x00)
                                    nn = nn + ".!";
                                else
                                {
                                    nn = nn.Substring(1, (int)l2) + "!." + nn.Substring((int)(l2 + 1), (int)(nn.Length - l2));
                                }
                            } while (l0 != 0 && nn.Length <= 0xfb);
                        }
                        else if (res == 0x80)
                        {
                            instcnt++;

                            // Rename file
                            f = File.OpenWrite("WISE" + Hexa(BitConverter.GetBytes(l1), 4));
                        }
                    }
                }

                df.Close();
                bf.Close();
                writestatus("Job done");
            }
            else
            {
                writestatus("Scriptfile not found");
            }
        }

        public void Run(string name, string dir, string? options = null)
        {
            Directory.CreateDirectory(dir);
            olddir = Environment.CurrentDirectory.TrimEnd('\\');
            Environment.CurrentDirectory = dir;
            _rollback = 0;
            unchecked { _userOffset = (uint)-1; }
            _renaming = true;
            if (options != null)
            {
                int b = 1;
                while (b <= options.Length)
                {
                    if (char.ToUpperInvariant(options[b]) == 'B')
                    {
                        _rollback = Deci(options.Substring(b + 1, 4));
                        b += 4;
                    }
                    else if (char.ToUpperInvariant(options[b]) == 'U')
                    {
                        _userOffset = Deci(options.Substring(b + 1, 4));
                        b += 4;
                    }
                    else if (char.ToUpperInvariant(options[b]) == 'R')
                    {
                        _renaming = false;
                    }
                    b++;
                }
            }

            if (OpenFile(name))
            {
                Approximate();
                if (!_pkzip)
                {
                    if (_userOffset >= 0)
                        scrnv.offsa = _userOffset;
                    else
                        scrnv.offsa -= _rollback;

                    FindReal();
                    if (_realfound)
                    {
                        ExtractFiles();
                        if (_renaming)
                            RenameFiles();
                    }
                }
                else
                {
                    scrnv.offsr = scrnv.offsa;
                    ExtractFiles();
                    if (_renaming)
                        RenameFiles();
                }
                CloseFile();
            }

            Environment.CurrentDirectory = olddir;
        }

        #endregion
    }
}