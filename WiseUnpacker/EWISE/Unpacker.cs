using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Serialization.Wrappers;
using PE = SabreTools.Models.PortableExecutable;

namespace WiseUnpacker.EWISE
{
    internal class Unpacker : BaseUnpacker
    {
        #region Instance Variables

        /// <summary>
        /// Input file to read and extract
        /// </summary>
        private ReadOnlyCompositeStream _inputFile;

        /// <summary>
        /// Currently matching known format
        /// </summary>
        private FormatProperty _currentFormat;

        #endregion

        /// <summary>
        /// Create a new E_WISE unpacker
        /// </summary>
        public Unpacker(string file)
        {
            // Input path(s)
            if (!OpenFile(file, out var stream) || stream == null)
                throw new FileNotFoundException(nameof(file));

            // Default options
            _inputFile = stream;
            _currentFormat = new FormatProperty
            {
                ExecutableType = ExecutableType.Unknown,
                ExecutableOffset = 0,
                CodeSectionLength = 0,
            };
        }

        /// <summary>
        /// Create a new E_WISE unpacker
        /// </summary>
        public Unpacker(Stream stream)
        {
            // TODO: Validate that the stream is seekable
            // Default options
            _inputFile = new ReadOnlyCompositeStream(stream);
            _currentFormat = new FormatProperty
            {
                ExecutableType = ExecutableType.Unknown,
                ExecutableOffset = 0,
                CodeSectionLength = 0,
            };
        }

        /// <summary>
        /// Attempt to parse, extract, and rename all files from a WISE installer
        /// </summary>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public bool Run(string outputPath)
        {
            // Move to data and determine if this is a known format
            long dataBase = JumpToTheData();
            _inputFile!.Seek(dataBase + _currentFormat!.ExecutableOffset, SeekOrigin.Begin);
            for (int i = 0; i < FormatProperty.KnownFormats.Length; i++)
            {
                if (_currentFormat.Equals(FormatProperty.KnownFormats[i]))
                {
                    _currentFormat = FormatProperty.KnownFormats[i];
                    break;
                }
            }

            // No match means extraction cannot continue
            if (_currentFormat.ArchiveEnd == 0)
                return false;

            // Skip over the addditional DLL name, if expected
            long dataStart = _currentFormat.ExecutableOffset;
            if (_currentFormat.Dll)
            {
                byte[] dll = new byte[256];
                _inputFile.Read(dll, 0, 1);
                dataStart++;

                if (dll[0] != 0x00)
                {
                    _inputFile.Read(dll, 1, dll[0]);
                    dataStart += dll[0];

                    _ = _inputFile.ReadInt32();
                    dataStart += 4;
                }
            }

            // Check if flags are consistent
            if (!_currentFormat.NoCrc)
            {
                int flags = _inputFile.ReadInt32();
                if ((flags & 0x0100) != 0)
                    return false;
            }

            if (_currentFormat.ArchiveEnd > 0)
            {
                _inputFile.Seek(dataBase + dataStart + _currentFormat.ArchiveEnd, SeekOrigin.Begin);
                int archiveEndLoaded = _inputFile.ReadInt32();
                if (archiveEndLoaded != 0)
                    _currentFormat.ArchiveEnd = archiveEndLoaded + dataBase;
            }

            _inputFile.Seek(dataBase + dataStart + _currentFormat.ArchiveStart, SeekOrigin.Begin);

            // Skip over the initialization text, if expected
            if (_currentFormat.InitText)
            {
                byte[] waitingBytes = new byte[256];
                _inputFile.Read(waitingBytes, 0, 1);
                _inputFile.Read(waitingBytes, 1, waitingBytes[0]);
            }

            long offsetReal = _inputFile.Position;
            int extracted = ExtractFiles(_inputFile, outputPath, _currentFormat.NoCrc, offsetReal);
            RenameFiles(outputPath, extracted);

            Close();
            return true;
        }

        /// <summary>
        /// Jump to the .data section of an executable stream
        /// </summary>
        /// TODO: MZ-only is not supported
        private long JumpToTheData()
        {
            _currentFormat = new FormatProperty
            {
                ExecutableType = ExecutableType.Unknown,
                ExecutableOffset = 0, // dataStart
                CodeSectionLength = 0,
            };
            long dataBase = 0;

            // Try to read as NE
            var ne = NewExecutable.Create(_inputFile);
            if (ne != null)
            {
                _currentFormat.ExecutableType = ExecutableType.NE;
                _currentFormat.ExecutableOffset = ne.Model.Stub!.Header!.NewExeHeaderAddr;
                _currentFormat.CodeSectionLength = -1;
                _currentFormat.DataSectionLength = -1;
                return dataBase;
            }

            // Try to read as LE/LX
            var le = LinearExecutable.Create(_inputFile);
            if (le != null)
            {
                _currentFormat.ExecutableType = ExecutableType.LE;
                _currentFormat.ExecutableOffset = le.Model.Stub!.Header!.NewExeHeaderAddr;
                _currentFormat.CodeSectionLength = -1;
                _currentFormat.DataSectionLength = -1;
                return dataBase;
            }

            bool searchAgainAtEnd = true;
            do
            {
                // Reset the state
                searchAgainAtEnd = false;
                dataBase += _currentFormat.ExecutableOffset;
                _currentFormat.ExecutableOffset = 0;
                _currentFormat.ExecutableType = ExecutableType.Unknown;
                _inputFile!.Seek(dataBase + _currentFormat.ExecutableOffset, SeekOrigin.Begin);

                // Try to read as PE
                var pe = PortableExecutable.Create(_inputFile);
                if (pe != null)
                    _currentFormat.ExecutableType = ProcessPe(pe, dataBase, ref searchAgainAtEnd);
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
                    _currentFormat!.CodeSectionLength = section.VirtualSize;

                // Get the data section
                section = pe.GetFirstSection(".data");
                if (section != null)
                {
                    _currentFormat!.DataSectionLength = section.VirtualSize;
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

                _currentFormat!.ExecutableOffset = (int)(resource!.PointerToRawData + resource.SizeOfRawData);
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
            _inputFile?.Close();
        }

        /// <summary>
        /// Scan a section for executable data
        /// </summary>
        /// <returns>True if the section contained executable data, false otherwise</returns>
        private bool ScanSectionForExecutable(PortableExecutable pe, PE.SectionHeader? section, long dataBase)
        {
            // If the section is invalid
            if (section == null || section.SizeOfRawData <= 20000)
                return false;

            // Loop through the raw data and attempt to create an executable
            for (int f = 0; f <= 20000 - 0x80; f++)
            {
                _inputFile!.Seek(dataBase + section.PointerToRawData + f, SeekOrigin.Begin);

                // Read the MS-DOS header
                var mz = MSDOS.Create(_inputFile);
                if (mz?.Model?.Header == null)
                    continue;

                // If the header is not a valid stub
                var header = mz.Model.Header;
                if (header.HeaderParagraphSize < 4 || header.NewExeHeaderAddr < 0x40 || (header.RelocationItems != 0 && header.RelocationItems != 3))
                    continue;

                // Set the executable offset and seek
                _currentFormat!.ExecutableOffset = (int)section.PointerToRawData + f;
                _inputFile.Seek(dataBase + section.PointerToRawData + pe.Model.OptionalHeader!.ResourceTable!.Size, SeekOrigin.Begin);
                return true;
            }

            return false;
        }
    }
}
