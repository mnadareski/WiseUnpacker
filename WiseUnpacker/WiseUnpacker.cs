using System.IO;
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
        private FormatProperty? currentFormat;

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
            long dataBase = JumpToTheData();
            inputFile!.Seek(dataBase + currentFormat!.ExecutableOffset, SeekOrigin.Begin);
            for (int i = 0; i < FormatProperty.KnownFormats.Length; i++)
            {
                if (currentFormat.Equals(FormatProperty.KnownFormats[i]))
                {
                    currentFormat = FormatProperty.KnownFormats[i];
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
        private long JumpToTheData()
        {
            currentFormat = new FormatProperty
            {
                ExecutableType = ExecutableType.Unknown,
                ExecutableOffset = 0, // dataStart
                CodeSectionLength = 0,
            };
            long dataBase = 0;

            // Try to read as NE
            var ne = NewExecutable.Create(inputFile);
            if (ne != null)
            {
                currentFormat.ExecutableType = ExecutableType.NE;
                currentFormat.ExecutableOffset = ne.Model.Stub!.Header!.NewExeHeaderAddr;
                currentFormat.CodeSectionLength = -1;
                currentFormat.DataSectionLength = -1;
                return dataBase;
            }

            // Try to read as LE/LX
            var le = LinearExecutable.Create(inputFile);
            if (le != null)
            {
                currentFormat.ExecutableType = ExecutableType.LE;
                currentFormat.ExecutableOffset = le.Model.Stub!.Header!.NewExeHeaderAddr;
                currentFormat.CodeSectionLength = -1;
                currentFormat.DataSectionLength = -1;
                return dataBase;
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
                    currentFormat.ExecutableType = ProcessPe(pe, dataBase, ref searchAgainAtEnd);
            }
            while (searchAgainAtEnd);

            return dataBase;
        }

        /// <summary>
        /// Process a PE executable header
        /// </summary>
        private ExecutableType ProcessPe(PortableExecutable pe, long dataBase, ref bool searchAgainAtEnd)
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
                {
                    currentFormat!.DataSectionLength = section.VirtualSize;
                    bool containsExe = ScanSectionForExecutable(pe, section, dataBase);
                    if (containsExe)
                        searchAgainAtEnd = true;
                }

                // Get the rsrc section
                PE.SectionHeader? resource = null;
                section = pe.GetFirstSection(".rsrc");
                if (section != null)
                {
                    resource = section;
                    bool containsExe = ScanSectionForExecutable(pe, section, dataBase);
                    if (containsExe)
                        searchAgainAtEnd = true;
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

        /// <summary>
        /// Scan a section for executable data
        /// </summary>
        /// <returns>True if the section contained executable data, false otherwise</returns>
        private bool ScanSectionForExecutable(PortableExecutable pe, PE.SectionHeader? section, long dataBase)
        {
            // If we have an invalid section
            if (section == null || section.SizeOfRawData <= 20000)
                return false;

            // Loop through the raw data and attempt to create an executable
            for (int f = 0; f <= 20000 - 0x80; f++)
            {
                inputFile!.Seek(dataBase + section.PointerToRawData + f, SeekOrigin.Begin);

                // Read the MS-DOS header
                var mz = MSDOS.Create(inputFile);
                if (mz?.Model?.Header == null)
                    continue;

                // If we have a valid MS-DOS header but not stub
                var header = mz.Model.Header;
                if (header.HeaderParagraphSize < 4 || header.NewExeHeaderAddr < 0x40 || (header.RelocationItems != 0 && header.RelocationItems != 3))
                    continue;

                // Set the executable offset and seek
                currentFormat!.ExecutableOffset = (int)section.PointerToRawData + f;
                inputFile.Seek(dataBase + section.PointerToRawData + pe.Model.OptionalHeader!.ResourceTable!.Size, SeekOrigin.Begin);
                return true;
            }

            return false;
        }
    }
}
