using System.IO;
using SabreTools.IO.Streams;
using SabreTools.Serialization.Wrappers;
using PE = SabreTools.Models.PortableExecutable;
using static WiseUnpacker.Common;

namespace WiseUnpacker.EWISE
{
    internal sealed class Unpacker : IWiseUnpacker
    {
        #region Instance Variables

        /// <summary>
        /// Input file to read and extract
        /// </summary>
        private readonly ReadOnlyCompositeStream _inputFile;

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

        /// <inheritdoc/>
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

            // Get the overlay header and confirm values
            var overlayHeader = new WiseOverlayHeader(_inputFile);

            // Check if flags are consistent
#if NET20 || NET35
            if (((overlayHeader.Flags & WiseOverlayHeaderFlags.PK_ZIP) != 0) ^ _currentFormat.NoCrc)
#else
            if (overlayHeader.Flags.HasFlag(WiseOverlayHeaderFlags.PK_ZIP) ^ _currentFormat.NoCrc)
#endif
                return false;

            // Check the archive end
            if (_currentFormat.ArchiveEnd > 0)
            {
                if (overlayHeader.Eof != 0)
                    _currentFormat.ArchiveEnd = overlayHeader.Eof + dataBase;
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
                PortableExecutable? pe;
                try
                {
                    pe = PortableExecutable.Create(_inputFile);
                    if (pe != null)
                        _currentFormat.ExecutableType = ProcessPe(pe, dataBase, ref searchAgainAtEnd);
                }
                catch
                {
                    // Ignore exceptions for now
                }
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
