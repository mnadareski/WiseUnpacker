using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;

namespace WiseUnpacker
{
    /// <summary>
    /// Common methods for Wise installer unpackers
    /// </summary>
    internal static class Common
    {
        /// <summary>
        /// Open a potential WISE installer file and any additional files
        /// </summary>
        /// <returns>True if the file could be opened, false otherwise</returns>
        internal static bool OpenFile(string name, out ReadOnlyCompositeStream? stream)
        {
            // If the file exists as-is
            if (File.Exists(name))
            {
                var fileStream = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream = new ReadOnlyCompositeStream([fileStream]);

                // Strip the extension
                name = Path.GetFileNameWithoutExtension(name);
            }

            // If the base name was provided, try to open the associated exe
            else if (File.Exists($"{name}.exe"))
            {
                var fileStream = File.Open($"{name}.exe", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream = new ReadOnlyCompositeStream([fileStream]);
            }

            // Otherwise, the file cannot be opened
            else
            {
                stream = null;
                return false;
            }

            // Loop through and try to read all additional files
            byte fileno = 2;
            string extraPath = $"{name}.w{fileno:X}";
            while (File.Exists(extraPath))
            {
                var fileStream = File.Open(extraPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream.AddStream(fileStream);
                fileno++;
                extraPath = $"{name}.w{fileno:X}";
            }

            return true;
        }

        #region Extraction

        /// <summary>
        /// Extract all files to a directory
        /// </summary>
        internal static int ExtractFiles(ReadOnlyCompositeStream input, string outputPath, bool pkzip, long offset)
        {
            // Create the output directory to extract to
            Directory.CreateDirectory(outputPath);

            var inflater = new Inflater();

            // "Extracting files"
            int extracted = 0;
            long fileEnd = 0;
            var dumpFile = File.OpenWrite(Path.Combine(outputPath, "WISE0000"));
            input.Seek(offset, SeekOrigin.Begin);

            do
            {
                // Increment the number of extracted files
                extracted++;

                // Cache the current position as the file start
                long fileStart = input.Position;
                if (fileStart == input.Length - 1)
                    break;

                // Read PKZIP header values
                uint zipCrc = 0;
                uint zipSize = 0;
                if (pkzip)
                {
                    // Save only select values
                    _ = input.ReadUInt32LittleEndian(); // Signature
                    _ = input.ReadUInt16LittleEndian(); // Version
                    _ = input.ReadUInt16LittleEndian(); // Flags
                    _ = input.ReadUInt16LittleEndian(); // Compression method
                    _ = input.ReadUInt16LittleEndian(); // Modification time
                    _ = input.ReadUInt16LittleEndian(); // Modification date
                    zipCrc = input.ReadUInt32LittleEndian();
                    zipSize = input.ReadUInt32LittleEndian(); // Compressed size
                    _ = input.ReadUInt32LittleEndian(); // Uncompressed size
                    ushort filenameLength = input.ReadUInt16LittleEndian();
                    ushort extraLength = input.ReadUInt16LittleEndian();
                    if (filenameLength + extraLength > 0)
                        _ = input.ReadBytes(filenameLength + extraLength);

                    // Reset the file start position
                    fileStart = input.Position;
                }

                // Inflate the data to a new file
                bool inflated = inflater.Inflate(input, Path.Combine(outputPath, $"WISE{extracted:X4}"));

                // Use the checksums based on the flag
                if (pkzip)
                {
                    if (inflater.CRC != zipCrc)
                        break;

                    fileEnd = fileStart + zipSize;
                    input.Seek(fileEnd, SeekOrigin.Begin);
                }
                else
                {
                    fileEnd = fileStart + inflater.InputSize;
                    input.Seek(fileEnd, SeekOrigin.Begin);

                    uint deflateCrc = input.ReadUInt32LittleEndian();
                    if (inflater.CRC != deflateCrc)
                    {
                        input.Seek(-3, SeekOrigin.Current);
                        deflateCrc = input.ReadUInt32LittleEndian();
                        if (inflater.CRC != deflateCrc)
                            break;
                    }

                    fileEnd = input.Position;
                }

                // Write the starting offset of the file to the dumpfile
                dumpFile.Write(BitConverter.GetBytes(fileStart), 0, 4);

                // If we had an inflate error specifically
                if (!inflated)
                    break;
            } while (input.Position < input.Length - 5);

            // Write the ending offset for the last file to the dumpfile
            dumpFile.Write(BitConverter.GetBytes(fileEnd), 0, 4);
            dumpFile.Close();

            return extracted;
        }

        /// <summary>
        /// Rename files in the output directory
        /// </summary>
        internal static bool RenameFiles(string dir, int extracted)
        {
            string nn = string.Empty;
            uint fileOffset2, scriptOffset1, scriptOffset2;
            long l0, fileOffset1, offs;

            // "Searching for script file"
            uint res;
            uint instcnt = 0;

            // Search for the script file
            uint fileno = 0;
            Stream? scriptFile = SearchForScriptFile(dir, extracted, ref fileno);
            if (scriptFile == null || fileno >= 6 || fileno >= extracted)
                return false;

            // Open the dumpfile
            string dumpFilePath = Path.Combine(dir, "WISE0000");
            uint[] dumpFileOffsets;
            using (var dumpFileStream = File.Open(dumpFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                dumpFileOffsets = ParseDumpFile(dumpFileStream);
            }

            // Calculate the offset shift value
            long entry = dumpFileOffsets.Length - 1, shift, shiftCheck = 0;
            bool shiftFound;
            do
            {
                // Get the first shift value
                shift = SearchForOffsetShift(scriptFile, dumpFileOffsets, out shiftFound, ref entry);

                // If a valid shift value was found, get the next shift to compare
                if (shiftFound)
                    shiftCheck = SearchForOffsetShift(scriptFile, dumpFileOffsets, out shiftFound, ref entry);

            } while (entry > 0 && (!shiftFound || shift != shiftCheck));

            // If the offset shift could not be calculated
            if (!shiftFound)
            {
                scriptFile.Close();
                return false;
            }

            // Rename the files
            long dumpEntryOffset = 1;
            while (dumpEntryOffset + 2 < dumpFileOffsets.Length)
            {
                // Read the current entry and next entry offsets
                dumpEntryOffset += 1;
                fileOffset1 = dumpFileOffsets[dumpEntryOffset + 0];
                fileOffset2 = dumpFileOffsets[dumpEntryOffset + 1];
                l0 = -1; // l0 = 0xffffffff;
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
                            if (nextChar == 0x00)
                            {
                                res = 0;
                                break;
                            }

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
                entry = dumpEntryOffset + 1;
                if (res == 0)
                {
                    // Sanitize the new file path
                    nn = nn.Replace("%", string.Empty);
                    nn = nn.Replace("\\", "/");

                    string oldfile = Path.Combine(dir, $"WISE{entry:X4}");
                    string newfile = Path.Combine(dir, nn);

                    // Make directories
                    var dirname = Path.GetDirectoryName(newfile);
                    if (dirname != null)
                        Directory.CreateDirectory(dirname);

                    // Ensure no overwrites
                    int postfix = 0;
                    while (File.Exists(newfile))
                    {
                        newfile = Path.Combine(dir, $"{nn}_{postfix++}");
                    }

                    // Rename file
                    File.Move(oldfile, newfile);
                }
                else if (res == 0x80)
                {
                    instcnt++;
                    string oldfile = Path.Combine(dir, $"WISE{entry:X4}");
                    string newfile = Path.Combine(dir, $"INST{instcnt:X4}");

                    // Ensure no overwrites
                    int postfix = 0;
                    while (File.Exists(newfile))
                    {
                        newfile = Path.Combine(dir, $"INST{instcnt:X4}_{postfix++}");
                    }

                    // Rename file
                    File.Move(oldfile, newfile);
                }
            }

            // Close the script and dump files
            scriptFile.Close();

            return true;
        }

        /// <summary>
        /// Raed a dumpfile and parse out the offsets
        /// </summary>
        private static uint[] ParseDumpFile(Stream dumpFile)
        {
            List<uint> offsets = [];

            long length = dumpFile.Length;
            while (length > 0)
            {
                uint offset = dumpFile.ReadUInt32LittleEndian();
                offsets.Add(offset);
                length -= 4;
            }

            return [.. offsets];
        }

        /// <summary>
        /// Compare an entry in a scriptfile and dumpfile offsets to get an offset shift, if possible
        /// </summary>
        private static long SearchForOffsetShift(Stream scriptFile, uint[] dumpFileOffsets, out bool found, ref long entry)
        {
            // Create variables for the two offsets
            uint dumpOffset1;
            uint scriptOffset1 = 0;

            // Search for the offset shift
            do
            {
                // Get the real offset values from the dump file
                dumpOffset1 = dumpFileOffsets[entry - 1];
                uint dumpOffset2 = dumpFileOffsets[entry - 0];

                // Start at the end of the scriptfile
                long scriptPosition = scriptFile.Length - 0x07;

                // Attempt to align the offset values
                found = false;
                while (scriptPosition > 0 && !found)
                {
                    scriptPosition--;
                    scriptOffset1 = ReadDWORD(scriptFile, scriptPosition + 0x00);
                    uint scriptOffset2 = ReadDWORD(scriptFile, scriptPosition + 0x04);

                    // If the correct offset shift has been found
                    if (scriptOffset2 > scriptOffset1
                        && scriptOffset2 < dumpOffset2
                        && scriptOffset1 < dumpOffset1
                        && scriptOffset2 - scriptOffset1 == dumpOffset2 - dumpOffset1)
                    {
                        found = true;
                    }
                }

                // If the shift wasn't found, move back an entry
                if (!found)
                    entry--;
            } while (!found && entry > 0);

            return dumpOffset1 - scriptOffset1;
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
                    while (ReadByte(scriptFile, offset + 0) != '%' || ReadByte(scriptFile, offset + 1) != '\\')
                    {
                        offset++;

                        // If the end of the file has been reached
                        if (offset >= scriptFile.Length - 1)
                            break;
                    }

                    // If the value was not found
                    if (offset >= scriptFile.Length - 1)
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

        #endregion

        #region Reading

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
            return stream.ReadWORDLittleEndian();
        }

        /// <summary>
        /// Read a DWORD from a position in the stream
        /// </summary>
        private static uint ReadDWORD(Stream stream, long position)
        {
            stream.Seek(position, SeekOrigin.Begin);
            return stream.ReadDWORDLittleEndian();
        }

        #endregion
    }
}
