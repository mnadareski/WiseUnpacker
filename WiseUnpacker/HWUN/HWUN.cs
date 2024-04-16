using System;
using System.IO;
using SabreTools.IO;

namespace WiseUnpacker.HWUN
{
    internal class Unpacker
    {
        #region Instance Variables

        /// <summary>
        /// Input file to read and extract
        /// </summary>
        private ReadOnlyCompositeStream _inputFile;

        /// <summary>
        /// Number of bytes to roll back if data is not PKZIP
        /// </summary>
        private uint _rollback;

        /// <summary>
        /// User-provided offset to start looking for data if not PKZIP
        /// </summary>
        private uint _userOffset;

        /// <summary>
        /// Determines if files will be renamed after extraction
        /// </summary>
        private bool _renaming;

        #endregion

        /// <summary>
        /// Create a new HWUN unpacker
        /// </summary>
        public Unpacker(string file)
        {
            // Input path(s)
            _inputFile = new ReadOnlyCompositeStream();
            if (!OpenFile(file))
                throw new FileNotFoundException(nameof(file));

            // Default options
            _rollback = 0;
            unchecked { _userOffset = (uint)-1; }
            _renaming = true;
        }

        /// <summary>
        /// Create a new HWUN unpacker with options set
        /// </summary>
        public Unpacker(string file, string? options)
        {
            // Input path(s)
            _inputFile = new ReadOnlyCompositeStream();
            if (!OpenFile(file))
                throw new FileNotFoundException(nameof(file));

            // Default options
            _rollback = 0;
            unchecked { _userOffset = (uint)-1; }
            _renaming = true;

            // User-provided options
            ParseOptions(options);
        }

        /// <summary>
        /// Attempt to parse, extract, and rename all files from a WISE installer
        /// </summary>
        public bool Run(string dir)
        {
            // Create the output directory to extract to
            Directory.CreateDirectory(dir);

            // Run the approximation
            long approxOffset = Approximate(out bool pkzip);

            // If the data is not PKZIP
            if (!pkzip)
            {
                // Reset the approximate offset
                if (_userOffset >= 0)
                    approxOffset = _userOffset;
                else
                    approxOffset -= _rollback;

                bool realFound = FindReal(dir, approxOffset, out long realOffset);
                if (realFound)
                {
                    int extracted = ExtractFiles(dir, pkzip, realOffset);
                    if (_renaming)
                        RenameFiles(dir, extracted);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // Use the approximate offset as the real offset
                int extracted = ExtractFiles(dir, pkzip, approxOffset);
                if (_renaming)
                    RenameFiles(dir, extracted);
            }

            _inputFile.Close();
            return true;
        }

        #region Helpers

        /// <summary>
        /// Approximate the location of the WISE information
        /// </summary>
        private long Approximate(out bool pkzip)
        {
            // Read the first 0xC000 bytes into a buffer
            byte[] buf = new byte[0xC200];
            _inputFile.Seek(0x0000, SeekOrigin.Begin);
            _inputFile.Read(buf, 0, 0xC000);

            // Use the initial offset and search for non-zero values
            long approxOffset = 0xC000;
            while (((buf[approxOffset] != 0x00) || (buf[approxOffset + 1] != 0x00)) && approxOffset > 0x20)
            {
                // Decrement the offset
                approxOffset--;

                // If either value is non-zero, keep searching
                if (buf[approxOffset] != 0x00 || buf[approxOffset + 1] != 0x00)
                    continue;

                // Find the current number of zeroes
                int count = 0;
                for (int i = 0x01; i <= 0x20; i++)
                {
                    if (buf[approxOffset - i] == 0x00)
                        count++;
                }

                // If we have less than 4 zeroes, decrement and continue
                if (count < 0x04)
                    approxOffset -= 2;
            }

            // Move the approximate offset forward
            approxOffset += 2;

            // Move forward until we find a potential starting point
            while (buf[approxOffset + 3] == 0x00 && approxOffset + 4 < 0xC000)
            {
                approxOffset += 4;
            }

            // Read the potential real offset from the buffer
            if (buf[approxOffset] <= 0x20 && buf[approxOffset + 1] > 0x00 && buf[approxOffset + 1] + approxOffset + 3 < 0xC000)
            {
                uint value = (uint)(buf[approxOffset + 1] + 0x02);
                int count = 0x00;
                for (int i = 0x02; i <= value - 0x01; i++)
                {
                    if (buf[approxOffset + i] >= 0x80)
                        count++;
                }

                if (count * 0x100 / value < 0x10)
                    approxOffset += value;
            }

            // Search for a zip signature
            long offset = 0x02;
            uint signature = 0x00;
            while (signature != 0x04034B50 && offset < 0x80 && approxOffset - offset >= 0 && approxOffset - offset <= 0xBFFC)
            {
                signature = BitConverter.ToUInt32(buf, (int)(approxOffset - offset));
                offset++;
            }

            // If the zip signature was found
            if (offset < 0x80)
            {
                pkzip = true;
                offset = 0x00;
                signature = 0x00;

                // Double-check the signature value is correctly set
                // TODO: Determine why this secondary check is done
                while (signature != 0x04034B50 && offset < approxOffset)
                {
                    signature = BitConverter.ToUInt32(buf, (int)offset);
                    offset++;
                }

                // Decrement the offset and set the new approximate offset
                offset--;
                approxOffset = offset;
                if (signature != 0x04034B50)
                    pkzip = false;
            }
            else
            {
                pkzip = false;
            }

            return approxOffset;
        }

        /// <summary>
        /// Extract all files to a directory
        /// </summary>
        private int ExtractFiles(string dir, bool pkzip, long offset)
        {
            uint newcrc = 0;
            var inflater = new InflateImpl();

            // "Extracting files"
            int extracted = 0;
            long fileEnd;
            var dumpFile = File.OpenWrite(Path.Combine(dir, "WISE0000"));
            _inputFile.Seek(offset, SeekOrigin.Begin);

            do
            {
                // Increment the number of extracted files
                extracted++;

                // Cache the current position as the file start
                long fileStart = _inputFile.Position;

                // Read PKZIP header values
                if (pkzip)
                {
                    _ = _inputFile.ReadBytes(0x0E);
                    newcrc = _inputFile.ReadUInt32();
                    _ = _inputFile.ReadBytes(0x08);
                    ushort len1 = _inputFile.ReadUInt16();
                    ushort len2 = _inputFile.ReadUInt16();
                    if (len1 + len2 > 0)
                        _ = _inputFile.ReadBytes(len1 + len2);
                }

                // Inflate the data to a new file
                inflater.Inflate(_inputFile, Path.Combine(dir, $"WISE{extracted:X4}"));
                if (pkzip)
                    inflater.CRC = 0x04034b50;

                // Set the file end
                fileEnd = fileStart + inflater.InputSize - 1;

                // If no inflation error occurred
                if (inflater.Result == 0x0000)
                {
                    // Read the new CRC
                    newcrc = _inputFile.ReadUInt32();

                    // Attempt to find the correct CRC value
                    uint attempt = 0;
                    while (inflater.CRC != newcrc && attempt < 8 && _inputFile.Position + 1 < _inputFile.Length)
                    {
                        _inputFile.Seek(-3, SeekOrigin.Current);
                        newcrc = _inputFile.ReadUInt32();
                        attempt++;
                    }

                    // Set the real file end
                    fileEnd = _inputFile.Position - 1;
                    if (pkzip)
                    {
                        fileEnd -= 4;
                        _inputFile.Seek(-4, SeekOrigin.Current);
                    }
                }

                // If an error occurred or the CRC does not match
                if (inflater.Result != 0x0000 || newcrc != inflater.CRC)
                {
                    inflater.CRC = 0xffffffff;
                    newcrc = 0xfffffffe;
                }

                // Write the starting offset of the file to the dumpfile
                dumpFile.Write(BitConverter.GetBytes(fileStart), 0, 4);
            } while (newcrc != inflater.CRC);

            // Write the ending offset for the last file to the dumpfile
            dumpFile.Write(BitConverter.GetBytes(fileEnd), 0, 4);
            dumpFile.Close();

            return extracted;
        }

        /// <summary>
        /// Find the real offset for non-zipped contents
        /// </summary>
        private bool FindReal(string dir, long approxOffset, out long realOffset)
        {
            realOffset = 0x00;
            uint newcrc = 0;
            long pos;
            var inflater = new InflateImpl();

            // Reset the offset if out of bounds
            if (approxOffset < 0x100)
                approxOffset = 0x100;
            else if (approxOffset > 0xBF00)
                approxOffset = 0xBF00;

            // If the approximate offset is a valid WORD value
            if (approxOffset >= 0x0000 && approxOffset <= 0xFFFF)
            {
                // Attempt to find the real first file by inflating blocks
                pos = 0x0000;
                do
                {
                    _inputFile.Seek(approxOffset + pos, SeekOrigin.Begin);
                    inflater.Inflate(_inputFile, Path.Combine(dir, "WISE0001"));
                    newcrc = _inputFile.ReadUInt32();
                    realOffset = approxOffset + pos;
                    pos++;
                } while ((inflater.CRC != newcrc || inflater.Result != 0x0000 || newcrc == 0x00000000) && pos != 0x100);

                // Try to find the ending position based on a valid CRC
                if ((inflater.CRC != newcrc || newcrc == 0x00000000 || inflater.Result != 0x0000) && pos == 0x100)
                {
                    pos = -1;
                    do
                    {
                        _inputFile.Seek(approxOffset + pos, SeekOrigin.Begin);
                        inflater.Inflate(_inputFile, Path.Combine(dir, "WISE0001"));
                        newcrc = _inputFile.ReadUInt32();
                        realOffset = approxOffset + pos;
                        pos--;
                    } while ((inflater.CRC != newcrc || inflater.Result != 0x0000 || newcrc == 0x00000000) && pos != -0x100);
                }
            }
            else
            {
                inflater.CRC = ~newcrc;
                pos = -0x100;
            }

            // Check for indicators of no WISE installer
            if ((inflater.CRC != newcrc || newcrc == 0x00000000 || inflater.Result != 0x0000) && pos == -0x100)
            {
                realOffset = 0xFFFFFFFF;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Convert a hex string to a DWORD value
        /// </summary>
        private static uint HexStringToDWORD(string value)
        {
            try
            {
                return uint.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Open a potential WISE installer file and any additional files
        /// </summary>
        /// <returns>True if the file could be opened, false otherwise</returns>
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
            string extraPath = $"{name}.w{fileno:X}";
            while (File.Exists(extraPath))
            {
                var fileStream = File.Open(extraPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _inputFile.AddStream(fileStream);
                fileno++;
                extraPath = $"{name}.w{fileno:X}";
            }

            return true;
        }

        /// <summary>
        /// Parse options from an input string
        /// </summary>
        private void ParseOptions(string? options)
        {
            // Ignore empty options
            if (string.IsNullOrEmpty(options))
                return;

            int b = 1;
            while (b <= options!.Length)
            {
                if (char.ToUpperInvariant(options[b]) == 'B')
                {
                    _rollback = HexStringToDWORD(options.Substring(b + 1, 4));
                    b += 4;
                }
                else if (char.ToUpperInvariant(options[b]) == 'U')
                {
                    _userOffset = HexStringToDWORD(options.Substring(b + 1, 4));
                    b += 4;
                }
                else if (char.ToUpperInvariant(options[b]) == 'R')
                {
                    _renaming = false;
                }

                b++;
            }
        }

        /// <summary>
        /// Read a byte from a position in the stream
        /// </summary>
        private static byte ReadByte(Stream stream, long position)
        {
            stream.Seek(position, SeekOrigin.Begin);
            return stream.ReadByteValue();
        }

        /// <summary>
        /// Read a WORD from a position in the stream
        /// </summary>
        private static ushort ReadWORD(Stream stream, long position)
        {
            stream.Seek(position, SeekOrigin.Begin);
            return stream.ReadUInt16();
        }

        /// <summary>
        /// Read a DWORD from a position in the stream
        /// </summary>
        private static uint ReadDWORD(Stream stream, long position)
        {
            stream.Seek(position, SeekOrigin.Begin);
            return stream.ReadUInt32();
        }

        /// <summary>
        /// Rename files in the output directory
        /// </summary>
        private void RenameFiles(string dir, int extracted)
        {
            Stream? bf = null;
            Stream? df = null;
            string nn = string.Empty;
            uint fileno;
            uint l2, l3 = 0, l4, res;
            long l, l0, l1 = 0, l5, offs, sh0 = 0, sh1 = 0;
            uint instcnt;
            Stream f;

            // "Searching for script file"
            fileno = 0;
            res = 1;
            instcnt = 0;
            while (fileno < extracted && fileno < 6 && res != 0)
            {
                fileno++;
                string bfPath = Path.Combine(dir, $"WISE{fileno:X4}");
                bf = File.Open(bfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                l = 0x0000;
                while (res != 0 && l < bf.Length)
                {
                    while (l < bf.Length && (ReadByte(bf, l + 0) != 0x25) || ReadByte(bf, l + 1) != 0x5c)
                        l++;
                }
                if (l < bf.Length)
                {
                    l1 = 0x01;
                    while (l1 < 0x40 && (ReadByte(bf, l - l1 + 0) != 0x25 || ReadByte(bf, l - l1 + 1) == 0x5c))
                        l1++;
                    if (l1 < 0x40)
                        res = 0;
                    else
                        l++;
                }

                if (res != 0)
                    bf.Close();
            }

            if (fileno < 6 && fileno < extracted)
            {
                // "Calculating offset shift value"
                string dfPath = Path.Combine(dir, "WISE0000");
                df = File.Open(dfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                l5 = (df.Length - 0x04) / 0x04;

                do
                {
                    do
                    {
                        l1 = ReadDWORD(df, l5 * 0x04 - 0x04);
                        l2 = ReadDWORD(df, l5 * 0x04 - 0x00);
                        l = bf!.Length - 0x07;
                        res = 1;
                        while (l >= 0 && res != 0)
                        {
                            l--;
                            l3 = ReadDWORD(bf, l + 0x00);
                            l4 = ReadDWORD(bf, l + 0x04);
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
                            l1 = ReadDWORD(df, l5 * 0x04 - 0x04);
                            l2 = ReadDWORD(df, l5 * 0x04 - 0x00);
                            l = bf.Length - 0x07;
                            l = 1;
                            while (l >= 0 && res != 0)
                            {
                                l--;
                                l3 = ReadDWORD(bf, l + 0x00);
                                l4 = ReadDWORD(bf, l + 0x04);
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
                    while (l5 + 8 < df.Length)
                    {
                        l5 += 0x04;
                        l1 = ReadDWORD(df, l5 + 0x00);
                        l2 = ReadDWORD(df, l5 + 0x04);
                        l0 = 0xffffffff;
                        res = 1;
                        while (l0 + 0x29 < bf.Length && res != 0)
                        {
                            l0++;
                            l3 = ReadDWORD(bf, l0 + 0x00);
                            l4 = ReadDWORD(bf, l0 + 0x04);
                            if ((l1 == l + sh0) && (l2 == l4 + sh0))
                                res = 0;
                        }

                        if (res == 0)
                        {
                            l2 = ReadWORD(bf, l0 - 2);
                            nn = "";
                            offs = l0;
                            l0 += 0x28;
                            res = 2;
                            if (ReadByte(bf, l0) == 0x25)
                            {
                                while (ReadByte(bf, l0) != 0)
                                {
                                    nn = nn + (char)ReadByte(bf, l0);
                                    if (ReadByte(bf, l0) < 0x20)
                                        res = 1;
                                    if (ReadByte(bf, l0) == 0x25 && res != 1)
                                        res = 3;
                                    if (ReadByte(bf, l0) == 0x5c && (ReadByte(bf, l0 - 1) == 0x25) && res == 3)
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
                            f = File.OpenWrite(Path.Combine(dir, $"WISE{l1:X4}"));

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
                            f = File.OpenWrite(Path.Combine(dir, $"WISE{l1:X4}"));
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

        #endregion
    }
}