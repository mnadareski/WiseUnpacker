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
using System.Text;

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

                // Read the MS-DOS header
                var executable = MSDOS.Create(inputFile);
                if (executable?.Model?.Header == null)
                    continue;

                // If we have a valid MS-DOS header but not stub
                var header = executable.Model.Header;
                if (header.HeaderParagraphSize < 4 || header.NewExeHeaderAddr < 0x40)
                    continue;

                // Set the executable offset and seek
                currentFormat.ExecutableOffset = header.NewExeHeaderAddr;
                inputFile.Seek(dataBase + currentFormat.ExecutableOffset, SeekOrigin.Begin);
                byte[] magic = inputFile.ReadBytes(4);
                string magicString = Encoding.ASCII.GetString(magic);

                // Handle 2-byte signatures
                switch (magicString.Substring(0, 2))
                {
                    case NE.Constants.SignatureString:
                        currentFormat.ExecutableType = ProcessNe();
                        break;

                    // TODO: Write new LE/LX handling
                    case LE.Constants.LESignatureString:
                    case LE.Constants.LXSignatureString:
                        currentFormat.ExecutableType = ProcessPe(ref searchAgainAtEnd);
                        break;

                    default:
                        break;
                }

                // Handle 4-byte signatures
                switch (magicString)
                {
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
                var ne = SabreTools.Serialization.Deserializers.NewExecutable.ParseExecutableHeader(inputFile);
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
    }
}
