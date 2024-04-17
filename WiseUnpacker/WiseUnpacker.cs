using System;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Serialization.Wrappers;
using WiseUnpacker.HWUN;
using MZ = SabreTools.Models.MSDOS;
using LE = SabreTools.Models.LinearExecutable;
using NE = SabreTools.Models.NewExecutable;
using PE = SabreTools.Models.PortableExecutable;
using System.Collections.Generic;

namespace WiseUnpacker
{
    public class WiseUnpacker
    {
        // IO values
        private ReadOnlyCompositeStream? inputFile;

        // Deterministic values
        private static readonly FormatProperty[] knownFormats = FormatProperty.GenerateKnownFormats();
        private FormatProperty? currentFormat;
        private long dataBase;

        /// <summary>
        /// Create a new heuristic unpacker
        /// </summary>
        public WiseUnpacker() { }

        /// <summary>
        /// Extract a file to an output using HWUN
        /// </summary>
        public bool ExtractToHWUN(string file, string outputPath, string? options = null)
        {
            var hwun = new Unpacker(file, options);
            return hwun.Run( outputPath);
        }

        /// <summary>
        /// Attempt to extract a Wise installer
        /// </summary>
        /// <param name="file">Possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        public bool ExtractTo(string file, string outputPath)
        {
            file = Path.GetFullPath(file);
            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(outputPath);

            if (!Open(file))
                return false;
                        
            // Move to data and determine if this is a known format
            JumpToTheData();
            inputFile!.Seek(dataBase + currentFormat!.ExecutableOffset, SeekOrigin.Begin);
            for (int i = 0; i < knownFormats.Length; i++)
            {
                if (currentFormat.Equals(knownFormats[i]))
                {
                    currentFormat = knownFormats[i];
                    break;
                }
            }

            // Fall back on heuristics if we couldn't match
            if (currentFormat.ArchiveEnd == 0)
                return ExtractToHWUN(file, outputPath);

            // Skip over the addditional DLL name, if we expect it
            long dataStart = currentFormat.ExecutableOffset;
            if (currentFormat.Dll)
            {
                byte[] dll = new byte[256];
                inputFile.Read(dll, 0, 1);
                dataStart++;

                if (dll[0] != 0x00)
                {
                    inputFile.Read(dll, 1, dll[0]);
                    dataStart += dll[0];

                    _ = inputFile.ReadInt32();
                    dataStart += 4;
                }
            }

            // Check if flags are consistent
            if (!currentFormat.NoCrc)
            {
                int flags = inputFile.ReadInt32();
                if ((flags & 0x0100) != 0)
                    return false;
            }

            if (currentFormat.ArchiveEnd > 0)
            {
                inputFile.Seek(dataBase + dataStart + currentFormat.ArchiveEnd, SeekOrigin.Begin);
                int archiveEndLoaded = inputFile.ReadInt32();
                if (archiveEndLoaded != 0)
                    currentFormat.ArchiveEnd = archiveEndLoaded + dataBase;
            }

            inputFile.Seek(dataBase + dataStart + currentFormat.ArchiveStart, SeekOrigin.Begin);

            // Skip over the initialization text, if we expect it
            if (currentFormat.InitText)
            {
                byte[] waitingBytes = new byte[256];
                inputFile.Read(waitingBytes, 0, 1);
                inputFile.Read(waitingBytes, 1, waitingBytes[0]);
            }

            long offsetReal = inputFile.Position;
            int extracted = ExtractFiles(outputPath, currentFormat.NoCrc, offsetReal);
            RenameFiles(outputPath, extracted);

            Close();
            return true;
        }

        /// <summary>
        /// Open a potentially-multipart file for analysis and extraction
        /// </summary>
        /// <param name="file">Possible wise installer base</param>
        /// <returns>True if the file could be opened, false otherwise</returns>
        private bool Open(string file)
        {
            var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            inputFile = new ReadOnlyCompositeStream([fileStream]);
            if (inputFile == null)
                return false;

            file = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(file!))!, Path.GetFileNameWithoutExtension(file));

            int fileno = 2;
            string extraFileName = $"{file}.w{fileno / 10 + 48}{fileno % 10 + 48}";

            while (File.Exists(extraFileName))
            {
                fileStream = File.Open(extraFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                inputFile.AddStream(fileStream);
                fileno++;
                extraFileName = $"{file}.w{fileno / 10 + 48}{fileno % 10 + 48}";
            }

            return true;
        }

        /// <summary>
        /// Jump to the .data section of an executable stream
        /// </summary>
        private void JumpToTheData()
        {
            currentFormat = new FormatProperty
            {
                ExecutableType = ExecutableType.Unknown,
                ExecutableOffset = 0, // dataStart
                CodeSectionLength = 0,
            };
            dataBase = 0;

            bool searchAgainAtEnd = true;
            do
            {
                searchAgainAtEnd = false;
                dataBase += currentFormat.ExecutableOffset;
                currentFormat.ExecutableOffset = 0;

                currentFormat.ExecutableType = ExecutableType.Unknown;
                inputFile!.Seek(dataBase + currentFormat.ExecutableOffset, SeekOrigin.Begin);
                var executable = MSDOS.Create(inputFile);

                if (executable?.Model?.Header != null
                    && (executable.Model.Header.Magic == PE.Constants.SignatureString || executable.Model.Header.Magic == MZ.Constants.SignatureString)
                    && executable.Model.Header.HeaderParagraphSize >= 4
                    && executable.Model.Header.NewExeHeaderAddr >= 0x40)
                {
                    currentFormat.ExecutableOffset = executable.Model.Header.NewExeHeaderAddr;
                    inputFile.Seek(dataBase + currentFormat.ExecutableOffset, SeekOrigin.Begin);
                    executable = MSDOS.Create(inputFile);
                }

                switch (executable?.Model?.Header?.Magic)
                {
                    case NE.Constants.SignatureString:
                        currentFormat.ExecutableType = ProcessNe();
                        break;
                    case LE.Constants.LESignatureString:
                    case LE.Constants.LXSignatureString:
                    case PE.Constants.SignatureString:
                        currentFormat.ExecutableType = ProcessPe(ref searchAgainAtEnd);
                        break;
                    default:
                        break;
                }
            }
            while (searchAgainAtEnd);
        }

        /// <summary>
        /// Process an NE executable header
        /// </summary>
        private ExecutableType ProcessNe()
        {
            try
            {
                inputFile!.Seek(dataBase + currentFormat!.ExecutableOffset, SeekOrigin.Begin);
                var ne = NewExecutable.Create(inputFile);
                if (ne == null)
                    return ExecutableType.Unknown;

                return ExecutableType.NE;
            }
            catch
            {
                return ExecutableType.Unknown;
            }
        }

        /// <summary>
        /// Process a PE executable header
        /// </summary>
        private ExecutableType ProcessPe(ref bool searchAgainAtEnd)
        {
            try
            {
                inputFile!.Seek(dataBase + currentFormat!.ExecutableOffset + 4, SeekOrigin.Begin);
                var pe = PortableExecutable.Create(inputFile);
                if (pe == null)
                    return ExecutableType.Unknown;

                // Get the text section
                var section = pe.GetFirstSection(".text");
                if (section != null)
                    currentFormat.CodeSectionLength = section.VirtualSize;

                // Get the data section
                section = pe.GetFirstSection(".data");
                if (section != null)
                    currentFormat.DataSectionLength = section.VirtualSize;

                // Get the rsrc section
                PE.SectionHeader? resource = null;
                section = pe.GetFirstSection(".rsrc");
                if (section != null)
                    resource = section;

                // Find the last section of .data or .rsrc if the relocations are not stripped
#if NET20 || NET35
                if ((pe.Model.COFFFileHeader!.Characteristics & PE.Characteristics.IMAGE_FILE_RELOCS_STRIPPED) == 0)
#else
                if (!pe.Model.COFFFileHeader!.Characteristics.HasFlag(PE.Characteristics.IMAGE_FILE_RELOCS_STRIPPED))
#endif
                {
                    PE.SectionHeader? temp = null;
                    for (int sectionNumber = 0; sectionNumber < (pe.SectionNames ?? []).Length; sectionNumber++)
                    {
                        // Get the section for the index
                        section = pe.GetSection(sectionNumber);
                        if (section?.Name == null)
                            continue;

                        // We only care about .data and .rsrc
                        switch (System.Text.Encoding.ASCII.GetString(section.Name).TrimEnd('\0'))
                        {
                            case ".data":
                            case ".rsrc":
                                temp = section;
                                break;

                            default:
                                break;
                        }
                    }

                    // The unpacker of the self-extractor does not use any resource functions either.
                    if (temp != null && temp.SizeOfRawData > 20000)
                    {
                        for (int f = 0; f <= 20000 - 0x80; f++)
                        {
                            inputFile.Seek(dataBase + temp.PointerToRawData + f, SeekOrigin.Begin);
                            var mz = MSDOS.Create(inputFile);

                            if (mz?.Model?.Header != null
                                && (mz.Model.Header.Magic == PE.Constants.SignatureString || mz.Model.Header.Magic == MZ.Constants.SignatureString)
                                && mz.Model.Header.HeaderParagraphSize >= 4
                                && mz.Model.Header.NewExeHeaderAddr >= 0x40
                                && (mz.Model.Header.RelocationItems == 0 || mz.Model.Header.RelocationItems == 3))
                            {
                                currentFormat.ExecutableOffset = (int)temp.PointerToRawData + f;
                                _ = (int)(dataBase + temp.PointerToRawData + pe.Model.OptionalHeader!.ResourceTable!.Size);
                                searchAgainAtEnd = true;
                                break;
                            }
                        }
                    }
                }

                currentFormat.ExecutableOffset = (int)(resource!.PointerToRawData + resource.SizeOfRawData);
                return ExecutableType.PE;
            }
            catch
            {
                return ExecutableType.Unknown;
            }
        }

        /// <summary>
        /// Close the possible Wise installer
        /// </summary>
        private void Close()
        {
            inputFile?.Close();
        }

        #region Copied from HWUN -- TODO: Remove redundant implementations

        /// <summary>
        /// Extract all files to a directory
        /// </summary>
        private int ExtractFiles(string dir, bool pkzip, long offset)
        {
            uint newcrc = 0;
            var inflater = new InflateImpl();

            // "Extracting files"
            int extracted = 0;
            long fileEnd = 0;
            var dumpFile = File.OpenWrite(Path.Combine(dir, "WISE0000"));
            inputFile!.Seek(offset, SeekOrigin.Begin);

            do
            {
                // Increment the number of extracted files
                extracted++;

                // Cache the current position as the file start
                long fileStart = inputFile.Position;
                if (fileStart == inputFile.Length - 1)
                    break;

                // Read PKZIP header values
                if (pkzip)
                {
                    _ = inputFile.ReadUInt32(); // Signature
                    _ = inputFile.ReadUInt16(); // Version
                    _ = inputFile.ReadUInt16(); // Flags
                    _ = inputFile.ReadUInt16(); // Compression
                    _ = inputFile.ReadUInt16(); // Modification time
                    _ = inputFile.ReadUInt16(); // Modification date
                    newcrc = inputFile.ReadUInt32();
                    _ = inputFile.ReadUInt32(); // Compressed size
                    _ = inputFile.ReadUInt32(); // Uncompressed size
                    ushort filenameLength = inputFile.ReadUInt16();
                    ushort extraLength = inputFile.ReadUInt16();
                    if (filenameLength + extraLength > 0)
                        _ = inputFile.ReadBytes(filenameLength + extraLength);
                }

                // Inflate the data to a new file
                bool inflated = inflater.Inflate(inputFile, Path.Combine(dir, $"WISE{extracted:X4}"));
                if (pkzip)
                    inflater.CRC = 0x04034b50;

                // Set the file end
                if (pkzip)
                    fileEnd = fileStart + 4;
                else
                    fileEnd = fileStart + inflater.InputSize - 1;

                // If no inflation error occurred
                if (inflated)
                {
                    // Read the new CRC or signature
                    inputFile.Seek(fileEnd, SeekOrigin.Begin);
                    newcrc = inputFile.ReadUInt32();

                    // Attempt to find the correct CRC value
                    uint attempt = 0;
                    while (inflater.CRC != newcrc && attempt < (pkzip ? int.MaxValue : 8) && inputFile.Position + 1 < inputFile.Length)
                    {
                        inputFile.Seek(-3, SeekOrigin.Current);
                        newcrc = inputFile.ReadUInt32();
                        attempt++;
                    }

                    // Set the real file end
                    fileEnd = inputFile.Position - 1;
                    if (pkzip)
                    {
                        fileEnd -= 4;
                        inputFile.Seek(-4, SeekOrigin.Current);
                        newcrc = 0xfffffffe;
                    }
                }

                // If an error occurred or the CRC does not match
                if (!inflated || newcrc != inflater.CRC)
                {
                    inflater.CRC = 0xffffffff;
                    newcrc = 0xfffffffe;
                }

                // Write the starting offset of the file to the dumpfile
                dumpFile.Write(BitConverter.GetBytes(fileStart), 0, 4);

                // If we had an inflate error specifically
                if (!inflated)
                    break;
            } while (newcrc != inflater.CRC && inputFile.Position < inputFile.Length - 5);

            // Write the ending offset for the last file to the dumpfile
            dumpFile.Write(BitConverter.GetBytes(fileEnd), 0, 4);
            dumpFile.Close();

            return extracted;
        }

        /// <summary>
        /// Raed a dumpfile and parse out the offsets
        /// </summary>
        public static uint[] ParseDumpFile(Stream dumpFile)
        {
            List<uint> offsets = [];

            long length = dumpFile.Length;
            while (length > 0)
            {
                uint offset = dumpFile.ReadUInt32();
                offsets.Add(offset);
                length -= 4;
            }

            return [.. offsets];
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

        #endregion
    }
}
