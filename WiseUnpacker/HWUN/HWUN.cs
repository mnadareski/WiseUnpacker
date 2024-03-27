using System;
using System.IO;
using static WiseUnpacker.HWUN.HexaDeci;

namespace WiseUnpacker.HWUN
{
    internal partial class HWUN
    {
        #region Instance Variables

        // State
        private MultipartFile? _inputFile;
        private Stream? _dumpFile;
        private bool _pkzip;
        private bool _realfound;

        // Extraction State
        private readonly InflateImpl inflater = new();
        public uint offsa;
        public uint offsr;
        public uint FileStart;
        public uint FileEnd;
        public uint Extracted;

        // Options
        private uint _rollback;
        private uint _userOffset;
        private bool _renaming;

        #endregion

        #region HWUN main section

        private string? olddir;

        private bool OpenFile(string name)
        {
            bool opened = MultipartFile.Open(Path.Combine(olddir!, name), out _inputFile);
            if (opened)
            {
                if (name[name.Length - 3] == '.')
                    name = name.Substring(0, name.Length - 4);
            }
            else
            {
                opened = MultipartFile.Open(Path.Combine(olddir!, $"{name}.exe"), out _inputFile);
            }

            if (opened)
            {
                byte fileno = 2;
                while (_inputFile!.Append(olddir + Path.DirectorySeparatorChar + name + ".w" + (char)(fileno / 10 + 48) + (char)(fileno % 10 + 48)))
                {
                    fileno++;
                }
            }

            return opened;
        }

        private void CloseFile()
        {
            _inputFile!.Close();
        }

        private void Approximate()
        {
            uint l0, l1;

            byte[] buf = new byte[0xc200];

            _inputFile!.Seek(0x0000);
            _inputFile!.BlockRead(buf, 0xc000);
            offsr = 0xbffc;

            uint l2 = 0;
            while (((buf[offsa] != 0x00) || (buf[offsa + 1] != 0x00)) && offsa > 0x20 && l2 != 1)
            {
                offsa--;
                if (buf[offsa] == 0x00 && buf[offsa + 1] == 0x00)
                {
                    l1 = 0;
                    for (l0 = 0x01; l0 <= 0x20; l0++)
                    {
                        if (buf[offsa - l0] == 0x00)
                            l1++;
                    }
                    if (l1 < 0x04)
                        offsa -= 2;
                }
            }

            offsa += 2;

            while (buf[offsa + 3] == 0x00 && offsa + 4 < 0xc000)
            {
                offsa += 4;
            }

            if (buf[offsa] <= 0x20 && buf[offsa + 1] > 0x00 && buf[offsa + 1] + offsa + 3 < 0xc000)
            {
                l1 = (uint)(buf[offsa + 1] + 0x02);
                l2 = 0x00;
                for (l0 = 0x02; l0 <= l1 - 0x01; l0++)
                {
                    if (buf[offsa + l0] >= 0x80)
                        l2++;
                }

                if (l2 * 0x100 / l1 < 0x10)
                    offsa += l1;
            }

            l0 = 0x02;
            while (l2 != 0x04034b50 && l0 < 0x80 && offsa - l0 >= 0 && offsa - l0 <= 0xbffc)
            {
                l2 = BitConverter.ToUInt32(buf, (int)(offsa - l0));
                l0++;
            }

            if (l0 < 0x80)
            {
                _pkzip = true;
                l0 = 0x0000;
                l1 = 0x0000;
                while (l1 != 0x04034b50 && l0 < offsa)
                {
                    l1 = BitConverter.ToUInt32(buf, (int)l0);
                    l0++;
                }

                l0--;
                offsa = l0;
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
                if (offsa < 0x100)
                    offsa = 0x100;
                else if (offsa >= 0xbf00)
                    offsa = 0xbf00;
            }

            if (offsa >= 0x0000 && offsa <= 0xffff)
            {
                // "Detecting real archive offset"
                pos = 0x0000;
                do
                {
                    _inputFile!.Seek(offsa + pos);
                    inflater.Inflate(_inputFile!, "WISE0001");
                    _inputFile!.BlockRead(newcrcbytes, 4);
                    newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                    offsr = offsa + pos;
                    pos++;
                } while ((inflater.CRC != newcrc || inflater.Result != 0x0000 || newcrc == 0x00000000) && pos != 0x100);

                if ((inflater.CRC != newcrc || newcrc == 0x00000000 || inflater.Result != 0x0000) && pos == 0x100)
                {
                    unchecked
                    {
                        pos = (uint)-1;
                        do
                        {
                            _inputFile!.Seek(offsa + pos);
                            inflater.Inflate(_inputFile!, "WISE0001");
                            _inputFile!.BlockRead(newcrcbytes, 4);
                            newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                            offsr = offsa + pos;
                            pos--;
                        } while ((inflater.CRC != newcrc || inflater.Result != 0x0000 || newcrc == 0x00000000) && pos != (uint)-0x100);
                    }
                }
            }
            else
            {
                inflater.CRC = ~newcrc;
                unchecked { pos = (uint)-0x100; }
            }

            unchecked
            {
                if ((inflater.CRC != newcrc || newcrc == 0x00000000 || inflater.Result != 0x0000) && pos == (uint)-0x100)
                {
                    // "ERROR: The file doesn''t seem to be a WISE installation"
                    _realfound = false;
                    offsr = 0xffffffff;
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

            // "Extracting files"
            _dumpFile = File.OpenWrite("WISE0000");
            _inputFile!.Seek(offsr);
            do
            {
                Extracted++;
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

                inflater.Inflate(_inputFile!, "WISE" + Hexa(Extracted));
                FileStart = fs;

                if (_pkzip)
                    inflater.CRC = 0x04034b50;

                FileEnd = FileStart + inflater.InputSize - 1;
                if (inflater.Result == 0x0000)
                {
                    _inputFile!.BlockRead(newcrcbytes, 4);
                    newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                    attempt = 0;
                    while (inflater.CRC != newcrc && attempt < 8 && _inputFile!.FilePosition + 1 < _inputFile!.FileLength)
                    {
                        _inputFile!.Seek(_inputFile!.FilePosition - 3);
                        _inputFile!.BlockRead(newcrcbytes, 4);
                        newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                        attempt++;
                    }

                    FileEnd = _inputFile!.FilePosition - 1;
                    if (_pkzip)
                    {
                        FileEnd -= 4;
                        _inputFile!.Seek(_inputFile!.FilePosition - 4);
                    }
                }

                if (inflater.Result != 0x0000 || newcrc != inflater.CRC)
                {
                    inflater.CRC = 0xffffffff;
                    newcrc = 0xfffffffe;
                }

                _dumpFile.Write(BitConverter.GetBytes(FileStart), 0, 4);
            } while (newcrc != inflater.CRC);

            _dumpFile.Write(BitConverter.GetBytes(FileEnd), 0, 4);
            _dumpFile.Close();
        }

        private void RenameFiles()
        {
            var bf = new BufferedFile();
            var df = new BufferedFile();
            string nn = string.Empty;
            uint fileno;
            uint sh0 = 0, sh1 = 0, offs, l, l0, l1 = 0, l2, l3 = 0, l4, l5, res;
            uint instcnt;
            Stream f;

            // "Searching for script file"
            fileno = 0;
            res = 1;
            instcnt = 0;
            while (fileno < Extracted && fileno < 6 && res != 0)
            {
                fileno++;
                bf.Open("WISE" + Hexa(fileno));
                l = 0x0000;
                while (res != 0 && l < bf.FileSize)
                {
                    while (l < bf.FileSize && (bf.ReadByte(l + 0) != 0x25) || bf.ReadByte(l + 1) != 0x5c)
                        l++;
                }
                if (l < bf.FileSize)
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

            if (fileno < 6 && fileno < Extracted)
            {
                // "Calculating offset shift value"
                df.Open("WISE0000");
                l5 = (df.FileSize - 0x04) / 0x04;

                do
                {
                    do
                    {
                        l1 = df.ReadLongInt(l5 * 0x04 - 0x04);
                        l2 = df.ReadLongInt(l5 * 0x04 - 0x00);
                        l = bf.FileSize - 0x07;
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
                            l = bf.FileSize - 0x07;
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
                    // "Renaming files"
                    l5 = 0x04;
                    while (l5 + 8 < df.FileSize)
                    {
                        l5 += 0x04;
                        l1 = df.ReadLongInt(l5 + 0x00);
                        l2 = df.ReadLongInt(l5 + 0x04);
                        l0 = 0xffffffff;
                        res = 1;
                        while (l0 + 0x29 < bf.FileSize && res != 0)
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
                            f = File.OpenWrite("WISE" + Hexa(l1));

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
                            f = File.OpenWrite("WISE" + Hexa(l1));
                        }
                    }
                }

                df.Close();
                bf.Close();
                // "Job done"
            }
            else
            {
                // "Scriptfile not found"
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
                        offsa = _userOffset;
                    else
                        offsa -= _rollback;

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
                    offsr = offsa;
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