using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Serialization.Wrappers;
using WiseUnpacker.HWUN;
using PE = SabreTools.Models.PortableExecutable;

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
            return hwun.Run(outputPath);
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

            if (!Unpacker.OpenFile(file, out var stream) || stream == null)
                return false;

            // Move to data and determine if this is a known format
            inputFile = stream;
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
            int extracted = Unpacker.ExtractFiles(inputFile, outputPath, currentFormat.NoCrc, offsetReal);
            Unpacker.RenameFiles(outputPath, extracted);

            Close();
            return true;
        }

        /// <summary>
        /// Jump to the .data section of an executable stream
        /// </summary>
        /// TODO: MZ-only is not supported
        private void JumpToTheData()
        {
            currentFormat = new FormatProperty
            {
                ExecutableType = ExecutableType.Unknown,
                ExecutableOffset = 0, // dataStart
                CodeSectionLength = 0,
            };
            dataBase = 0;

            // Try to read as NE
            var ne = NewExecutable.Create(inputFile);
            if (ne != null)
            {
                currentFormat.ExecutableType = ExecutableType.NE;
                currentFormat.ExecutableOffset = ne.Model.Stub!.Header!.NewExeHeaderAddr;
                currentFormat.CodeSectionLength = -1;
                currentFormat.DataSectionLength = -1;
                return;
            }

            // Try to read as LE/LX
            var le = LinearExecutable.Create(inputFile);
            if (le != null)
            {
                currentFormat.ExecutableType = ExecutableType.LE;
                currentFormat.ExecutableOffset = le.Model.Stub!.Header!.NewExeHeaderAddr;
                currentFormat.CodeSectionLength = -1;
                currentFormat.DataSectionLength = -1;
                return;
            }

            bool searchAgainAtEnd = true;
            do
            {
                // Reset the state
                searchAgainAtEnd = false;
                dataBase += currentFormat.ExecutableOffset;
                currentFormat.ExecutableOffset = 0;
                currentFormat.ExecutableType = ExecutableType.Unknown;
                inputFile!.Seek(dataBase + currentFormat.ExecutableOffset, SeekOrigin.Begin);

                // Try to read as PE
                var pe = PortableExecutable.Create(inputFile);
                if (pe != null)
                    currentFormat.ExecutableType = ProcessPe(pe, ref searchAgainAtEnd);
            }
            while (searchAgainAtEnd);
        }

        /// <summary>
        /// Process a PE executable header
        /// </summary>
        private ExecutableType ProcessPe(PortableExecutable pe, ref bool searchAgainAtEnd)
        {
            try
            {
                // Get the text section
                var section = pe.GetFirstSection(".text");
                if (section != null)
                    currentFormat!.CodeSectionLength = section.VirtualSize;

                // Get the data section
                section = pe.GetFirstSection(".data");
                if (section != null)
                    currentFormat!.DataSectionLength = section.VirtualSize;

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
                        switch (Encoding.ASCII.GetString(section.Name).TrimEnd('\0'))
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
                            inputFile!.Seek(dataBase + temp.PointerToRawData + f, SeekOrigin.Begin);

                            // Read the MS-DOS header
                            var mz = MSDOS.Create(inputFile);
                            if (mz?.Model?.Header == null)
                                continue;

                            // If we have a valid MS-DOS header but not stub
                            var header = mz.Model.Header;
                            if (header.HeaderParagraphSize < 4 || header.NewExeHeaderAddr < 0x40 || (header.RelocationItems != 0 && header.RelocationItems != 3))
                                continue;

                            // Set the executable offset and seek
                            currentFormat!.ExecutableOffset = (int)temp.PointerToRawData + f;
                            inputFile.Seek(dataBase + temp.PointerToRawData + pe.Model.OptionalHeader!.ResourceTable!.Size, SeekOrigin.Begin);
                            searchAgainAtEnd = true;
                            break;
                        }
                    }
                }

                currentFormat!.ExecutableOffset = (int)(resource!.PointerToRawData + resource.SizeOfRawData);
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
    }
}
