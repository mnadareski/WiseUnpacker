using System;
using System.IO;
using SabreTools.IO;
using static WiseUnpacker.HWUN.HexaDeci;

namespace WiseUnpacker.HWUN
{
    internal class Unpacker
    {
        #region Instance Variables

        // State
        private ReadOnlyCompositeStream? _inputFile;
        private Stream? _dumpFile;
        private bool _pkzip;
        private bool _realfound;

        // Extraction State
        private readonly InflateImpl inflater = new();
        private uint _approxOffset;
        private uint _realOffset;
        private long _fileStart;
        private long _fileEnd;
        private uint _extracted;

        // Options
        private uint _rollback;
        private uint _userOffset;
        private bool _renaming;

        #endregion

        #region HWUN main section

        private bool OpenFile(string name)
        {
            // If the file exists as-is
            if (File.Exists(name))
            {
                var fileStream = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _inputFile = new ReadOnlyCompositeStream([fileStream]);

                // Strip the extension
                name = Path.GetFileNameWithoutExtension(name);
            }

            // If the base name was provided, try to open the associated exe
            else if (File.Exists($"{name}.exe"))
            {
                var fileStream = File.Open($"{name}.exe", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _inputFile = new ReadOnlyCompositeStream([fileStream]);
            }

            // Otherwise, the file cannot be opened
            else
            {
                return false;
            }

            // Loop through and try to read all additional files
            byte fileno = 2;
            string extraPath = $"{name}.w{fileno:x}";
            while (File.Exists(extraPath))
            {
                var fileStream = File.Open(extraPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _inputFile.AddStream(fileStream);
                fileno++;
                extraPath = $"{name}.w{fileno:x}";
            }

            return true;
        }

        private void CloseFile()
        {
            _inputFile!.Close();
        }

        private void Approximate()
        {
            uint l0, l1;

            byte[] buf = new byte[0xc200];

            _inputFile!.Seek(0x0000, SeekOrigin.Begin);
            _inputFile!.Read(buf, 0, 0xc000);
            _realOffset = 0xbffc;

            uint l2 = 0;
            while (((buf[_approxOffset] != 0x00) || (buf[_approxOffset + 1] != 0x00)) && _approxOffset > 0x20 && l2 != 1)
            {
                _approxOffset--;
                if (buf[_approxOffset] == 0x00 && buf[_approxOffset + 1] == 0x00)
                {
                    l1 = 0;
                    for (l0 = 0x01; l0 <= 0x20; l0++)
                    {
                        if (buf[_approxOffset - l0] == 0x00)
                            l1++;
                    }
                    if (l1 < 0x04)
                        _approxOffset -= 2;
                }
            }

            _approxOffset += 2;

            while (buf[_approxOffset + 3] == 0x00 && _approxOffset + 4 < 0xc000)
            {
                _approxOffset += 4;
            }

            if (buf[_approxOffset] <= 0x20 && buf[_approxOffset + 1] > 0x00 && buf[_approxOffset + 1] + _approxOffset + 3 < 0xc000)
            {
                l1 = (uint)(buf[_approxOffset + 1] + 0x02);
                l2 = 0x00;
                for (l0 = 0x02; l0 <= l1 - 0x01; l0++)
                {
                    if (buf[_approxOffset + l0] >= 0x80)
                        l2++;
                }

                if (l2 * 0x100 / l1 < 0x10)
                    _approxOffset += l1;
            }

            l0 = 0x02;
            while (l2 != 0x04034b50 && l0 < 0x80 && _approxOffset - l0 >= 0 && _approxOffset - l0 <= 0xbffc)
            {
                l2 = BitConverter.ToUInt32(buf, (int)(_approxOffset - l0));
                l0++;
            }

            if (l0 < 0x80)
            {
                _pkzip = true;
                l0 = 0x0000;
                l1 = 0x0000;
                while (l1 != 0x04034b50 && l0 < _approxOffset)
                {
                    l1 = BitConverter.ToUInt32(buf, (int)l0);
                    l0++;
                }

                l0--;
                _approxOffset = l0;
                if (l1 != 0x04034b50)
                    _pkzip = false;
            }
            else
            {
                _pkzip = false;
            }
        }

        private void FindReal(string dir)
        {
            byte[] newcrcbytes = new byte[4];
            uint newcrc = 0, pos;

            if (!_pkzip)
            {
                if (_approxOffset < 0x100)
                    _approxOffset = 0x100;
                else if (_approxOffset >= 0xbf00)
                    _approxOffset = 0xbf00;
            }

            if (_approxOffset >= 0x0000 && _approxOffset <= 0xffff)
            {
                // "Detecting real archive offset"
                pos = 0x0000;
                do
                {
                    _inputFile!.Seek(_approxOffset + pos, SeekOrigin.Begin);
                    inflater.Inflate(_inputFile!, Path.Combine(dir, "WISE0001"));
                    _inputFile!.Read(newcrcbytes, 0, 4);
                    newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                    _realOffset = _approxOffset + pos;
                    pos++;
                } while ((inflater.CRC != newcrc || inflater.Result != 0x0000 || newcrc == 0x00000000) && pos != 0x100);

                if ((inflater.CRC != newcrc || newcrc == 0x00000000 || inflater.Result != 0x0000) && pos == 0x100)
                {
                    unchecked
                    {
                        pos = (uint)-1;
                        do
                        {
                            _inputFile!.Seek(_approxOffset + pos, SeekOrigin.Begin);
                            inflater.Inflate(_inputFile!, Path.Combine(dir, "WISE0001"));
                            _inputFile!.Read(newcrcbytes, 0, 4);
                            newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                            _realOffset = _approxOffset + pos;
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
                    _realOffset = 0xffffffff;
                }
                else
                {
                    _realfound = true;
                }
            }
        }

        private void ExtractFiles(string dir)
        {
            uint newcrc = 0;
            byte[] newcrcbytes = new byte[4];
            uint attempt;
            long fs;
            byte[] buf = new byte[0x400];
            ushort len1, len2;
            byte[] len1bytes = new byte[2], len2bytes = new byte[2];

            // "Extracting files"
            _dumpFile = File.OpenWrite(Path.Combine(dir, "WISE0000"));
            _inputFile!.Seek(_realOffset, SeekOrigin.Begin);
            do
            {
                _extracted++;
                fs = _inputFile!.Position;
                if (_pkzip)
                {
                    _inputFile!.Read(buf, 0, 0xe);
                    _inputFile!.Read(newcrcbytes, 0, 0x04);
                    newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                    _inputFile!.Read(buf, 0, 0x08);
                    _inputFile!.Read(len1bytes, 0, 0x02);
                    len1 = BitConverter.ToUInt16(len1bytes, 0);
                    _inputFile!.Read(len2bytes, 0, 0x02);
                    len2 = BitConverter.ToUInt16(len2bytes, 0);
                    if (len1 + len2 > 0)
                        _inputFile!.Read(buf, 0, (ushort)(len1 + len2));
                }

                inflater.Inflate(_inputFile!, Path.Combine(dir, $"WISE{Hexa(_extracted)}"));
                _fileStart = fs;

                if (_pkzip)
                    inflater.CRC = 0x04034b50;

                _fileEnd = _fileStart + inflater.InputSize - 1;
                if (inflater.Result == 0x0000)
                {
                    _inputFile!.Read(newcrcbytes, 0, 4);
                    newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                    attempt = 0;
                    while (inflater.CRC != newcrc && attempt < 8 && _inputFile!.Position + 1 < _inputFile!.Length)
                    {
                        _inputFile!.Seek(-3, SeekOrigin.Current);
                        _inputFile!.Read(newcrcbytes, 0, 4);
                        newcrc = BitConverter.ToUInt32(newcrcbytes, 0);
                        attempt++;
                    }

                    _fileEnd = _inputFile!.Position - 1;
                    if (_pkzip)
                    {
                        _fileEnd -= 4;
                        _inputFile!.Seek(-4, SeekOrigin.Current);
                    }
                }

                if (inflater.Result != 0x0000 || newcrc != inflater.CRC)
                {
                    inflater.CRC = 0xffffffff;
                    newcrc = 0xfffffffe;
                }

                _dumpFile.Write(BitConverter.GetBytes(_fileStart), 0, 4);
            } while (newcrc != inflater.CRC);

            _dumpFile.Write(BitConverter.GetBytes(_fileEnd), 0, 4);
            _dumpFile.Close();
        }

        private void RenameFiles(string dir)
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
            while (fileno < _extracted && fileno < 6 && res != 0)
            {
                fileno++;
                bf.Open(Path.Combine(dir, $"WISE{Hexa(fileno)}"));
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

            if (fileno < 6 && fileno < _extracted)
            {
                // "Calculating offset shift value"
                df.Open(Path.Combine(dir, "WISE0000"));
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
                            f = File.OpenWrite(Path.Combine(dir, $"WISE{Hexa(l1)}"));

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
                                var tempout = File.OpenWrite(Path.Combine(dir, nn));
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
                            f = File.OpenWrite(Path.Combine(dir, $"WISE{Hexa(l1)}"));
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

        public bool Run(string name, string dir, string? options = null)
        {
            Directory.CreateDirectory(dir);
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

            if (!OpenFile(name))
                return false;

            Approximate();
            if (!_pkzip)
            {
                if (_userOffset >= 0)
                    _approxOffset = _userOffset;
                else
                    _approxOffset -= _rollback;

                FindReal(dir);
                if (_realfound)
                {
                    ExtractFiles(dir);
                    if (_renaming)
                        RenameFiles(dir);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                _realOffset = _approxOffset;
                ExtractFiles(dir);
                if (_renaming)
                    RenameFiles(dir);
            }

            CloseFile();
            return true;
        }

        #endregion
    }
}