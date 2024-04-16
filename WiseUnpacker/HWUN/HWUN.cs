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
        private bool RenameFiles(string dir, int extracted)
        {
            string nn = string.Empty;
            uint fileOffset2, scriptOffset1 = 0, scriptOffset2;
            long l0, fileOffset1 = 0, offs;

            // "Searching for script file"
            uint res = 1;
            uint instcnt = 0;

            // Search for the script file
            uint fileno = 0;
            Stream? scriptFile = SearchForScriptFile(dir, extracted, ref fileno);
            if (scriptFile == null || fileno >= 6 || fileno >= extracted)
                return false;

            // Open the dumpfile
            string dumpFilePath = Path.Combine(dir, "WISE0000");
            Stream dumpFile = File.Open(dumpFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            long entry = (dumpFile.Length - 0x04) / 0x04;

            // Calculate the offset shift value
            long shift, shiftCheck = 0;
            bool shiftFound = false;
            do
            {
                // Get the first shift value
                shift = SearchForOffsetShift(scriptFile, dumpFile, out shiftFound, ref entry);

                // If a valid shift value was found, get the next shift to compare
                if (shiftFound)
                    shiftCheck = SearchForOffsetShift(scriptFile, dumpFile, out shiftFound, ref entry);

            } while (entry > 0 && (!shiftFound || shift != shiftCheck));

            // If the offset shift could not be calculated
            if (!shiftFound)
            {
                scriptFile.Close();
                dumpFile.Close();
                return false;
            }

            // Rename the files
            long dumpEntryOffset = 0x04;
            while (dumpEntryOffset + 8 < dumpFile.Length)
            {
                // Read the current entry and next entry offsets
                dumpEntryOffset += 0x04;
                fileOffset1 = ReadDWORD(dumpFile, dumpEntryOffset + 0x00);
                fileOffset2 = ReadDWORD(dumpFile, dumpEntryOffset + 0x04);
                l0 = 0xffffffff;
                res = 1;

                // Find the name offset for this entry, if possible
                while (l0 + 0x29 < scriptFile.Length && res != 0)
                {
                    l0++;
                    scriptOffset1 = ReadDWORD(scriptFile, l0 + 0x00);
                    scriptOffset2 = ReadDWORD(scriptFile, l0 + 0x04);
                    if ((fileOffset1 == scriptOffset1 + shift) && (fileOffset2 == scriptOffset2 + shift))
                        res = 0;
                }

                // If a name offset was found
                if (res == 0)
                {
                    fileOffset2 = ReadWORD(scriptFile, l0 - 2);
                    nn = string.Empty;
                    offs = l0;
                    l0 += 0x28;
                    res = 2;

                    char nextChar = (char)ReadByte(scriptFile, l0);
                    if (nextChar == '%')
                    {
                        while (nextChar != 0)
                        {
                            nextChar = (char)ReadByte(scriptFile, l0);
                            nn += nextChar;
                            if (nextChar < 0x20)
                                res = 1;
                            if (nextChar == '%' && res != 1)
                                res = 3;
                            if (nextChar == '\\' && (ReadByte(scriptFile, l0 - 1) == '%') && res == 3)
                                res = 4;
                            if (res == 4)
                                res = 0;

                            l0++;
                        }
                    }

                    // If no valid name is found, mark as an Install file
                    if (res != 0)
                        res = 0x80;
                }

                // If a valid name was found at the offset
                entry = (dumpEntryOffset + 0x04) / 0x04;
                if (res == 0)
                {
                    // Sanitize the new file path
                    nn = nn.Replace("%", string.Empty);
                    string oldfile = Path.Combine(dir, $"WISE{entry:X4}");
                    string newfile = Path.Combine(dir, nn);

                    // Make directories
                    var dirname = Path.GetDirectoryName(newfile);
                    if (dirname != null)
                        Directory.CreateDirectory(dirname);

                    // Rename file
                    File.Move(oldfile, newfile);
                }
                else if (res == 0x80)
                {
                    instcnt++;
                    string oldfile = Path.Combine(dir, $"WISE{entry:X4}");
                    string newfile = Path.Combine(dir, $"INST{instcnt:X4}");

                    // Rename file
                    File.Move(oldfile, newfile);
                }
            }

            // Close the script and dump files
            scriptFile.Close();
            dumpFile.Close();

            return true;
        }

        /// <summary>
        /// Search the first 6 entries for a script file
        /// </summary>
        /// <remarks>
        /// The script file contains all of the strings and filenames that are used for
        /// the WISE installer. This method searches for a file that contains multiple
        /// instances of "%\", which usually come from strings that look like:
        /// "%MAINDIR%\INSTALL.LOG"
        /// </remarks>
        private static Stream? SearchForScriptFile(string dir, int extracted, ref uint fileno)
        {
            // Check for boundary cases first
            if (fileno >= extracted || fileno >= 6)
                return null;

            // Search up to the first 6 extacted files for a script file
            Stream? scriptFile = null;
            bool found = false;
            while (fileno < extracted && fileno < 6 && !found)
            {
                // Increment the current file number
                fileno++;

                // Open the generic-named file associated with the number
                string bfPath = Path.Combine(dir, $"WISE{fileno:X4}");
                scriptFile = File.Open(bfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                long offset = 0;
                while (!found && offset < scriptFile.Length)
                {
                    // Search for the first instance of "%\"
                    while (offset < scriptFile.Length && (ReadByte(scriptFile, offset + 0) != '%') || ReadByte(scriptFile, offset + 1) != '\\')
                    {
                        offset++;
                    }

                    // If the value was not found
                    if (offset >= scriptFile.Length)
                        break;

                    // Look for a previous entry in the script file
                    long offsetCheck = 0x01;
                    while (offsetCheck < 0x40 && (ReadByte(scriptFile, offset - offsetCheck + 0) != '%' || ReadByte(scriptFile, offset - offsetCheck + 1) == '\\'))
                    {
                        offsetCheck++;
                    }

                    // If a previous entry is found
                    if (offsetCheck < 0x40)
                        found = true;

                    // Otherwise, keep searching
                    else
                        offset++;
                }

                // Close the file if it wasn't the script file
                if (!found)
                    scriptFile.Close();
            }

            return scriptFile;
        }

        /// <summary>
        /// Compare an entry in a scriptfile and dumpfile to get an offset shift, if possible
        /// </summary>
        private static long SearchForOffsetShift(Stream scriptFile, Stream dumpFile, out bool found, ref long entry)
        {
            // Create variables for the two offsets
            uint dumpOffset;
            uint scriptOffset = 0;

            // Search for the offset shift
            do
            {
                // Get the real offset values from the dump file
                dumpOffset = ReadDWORD(dumpFile, entry * 0x04 - 0x04);
                uint nextDumpOffset = ReadDWORD(dumpFile, entry * 0x04 - 0x00);
                long scriptPosition = scriptFile!.Length - 0x07;
                found = false;

                // Attempt to align the offset values
                while (scriptPosition >= 0 && !found)
                {
                    scriptPosition--;
                    scriptOffset = ReadDWORD(scriptFile, scriptPosition + 0x00);
                    uint nextScriptOffset = ReadDWORD(scriptFile, scriptPosition + 0x04);

                    // If the correct offset shift has been found
                    if (nextScriptOffset > scriptOffset
                        && nextScriptOffset < nextDumpOffset
                        && scriptOffset < dumpOffset
                        && nextScriptOffset - scriptOffset == nextDumpOffset - dumpOffset)
                    {
                        found = true;
                    }
                }

                // If the shift wasn't found, move back an entry
                if (!found)
                    entry--;
            } while (!found && entry > 0);

            return dumpOffset - scriptOffset;
        }

        #endregion
    }
}