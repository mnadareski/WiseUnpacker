using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Serialization.Wrappers;
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
            JumpToTheData();
            _inputFile!.Seek(_currentFormat!.ExecutableOffset, SeekOrigin.Begin);
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

            // Check the archive end
            if (_currentFormat.ArchiveEnd > 0)
            {
                if (overlayHeader.Eof != 0)
                    _currentFormat.ArchiveEnd = overlayHeader.Eof;
            }

            // Get if the format is PKZIP packed or not
#if NET20 || NET35
            bool pkzip = (overlayHeader.Flags & WiseOverlayHeaderFlags.PK_ZIP) != 0;
#else
            bool pkzip = overlayHeader.Flags.HasFlag(WiseOverlayHeaderFlags.PK_ZIP);
#endif

            long offsetReal = _inputFile.Position;
            int extracted = ExtractFiles(_inputFile, outputPath, pkzip, offsetReal);
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
            _inputFile.Seek(0, SeekOrigin.Begin);
            var ne = NewExecutable.Create(_inputFile);
            if (ne?.Model?.SegmentTable != null)
            {
                _currentFormat.ExecutableType = ExecutableType.NE;

                if (ne.Model.SegmentTable.Length > 0)
                    _currentFormat.CodeSectionLength = ne.Model.SegmentTable[0]!.Length;
                if (ne.Model.SegmentTable.Length > 2)
                    _currentFormat.DataSectionLength = ne.Model.SegmentTable[2]!.Length;

                // Get the resource table offset and seek
                uint resourceTableOffset = ne.Model.Header!.ResourceTableOffset
                    + ne.Model.Stub!.Header!.NewExeHeaderAddr;
                _inputFile.Seek(resourceTableOffset, SeekOrigin.Begin);

                // Create an offset to set after
                long afterOffset = _inputFile.Position;
                
                // Get the offset immediately following the resource table
                ushort align = _inputFile.ReadUInt16LittleEndian();
                for (int i = 0; i < ne.Model.Header.ResourceEntriesCount; i++)
                {
                    // Parse the resource type header
                    _ = _inputFile.ReadUInt16LittleEndian(); // TypeID
                    ushort resourceCount = _inputFile.ReadUInt16LittleEndian();
                    _ = _inputFile.ReadUInt32LittleEndian(); // Reserved

                    // Parse the resource type entries
                    for (int j = 0; j < resourceCount; j++)
                    {
                        // Parse the resource entry header
                        ushort offset = _inputFile.ReadUInt16LittleEndian();
                        ushort length = _inputFile.ReadUInt16LittleEndian();
                        _ = _inputFile.ReadUInt16LittleEndian(); // FlagWord
                        _ = _inputFile.ReadUInt16LittleEndian(); // ResourceID
                        _ = _inputFile.ReadUInt32LittleEndian(); // Reserved

                        // Get the location of the value
                        int value = (offset << align) + (length << align);
                        if (value > afterOffset)
                            afterOffset = value;
                    }
                }

                _currentFormat.ExecutableOffset = afterOffset;
                return dataBase;
            }

            // Try to read as LE/LX
            _inputFile.Seek(0, SeekOrigin.Begin);
            var le = LinearExecutable.Create(_inputFile);
            if (le != null)
            {
                _currentFormat.ExecutableType = ExecutableType.LE;
                _currentFormat.ExecutableOffset = le.Model.Stub!.Header!.NewExeHeaderAddr;
                _currentFormat.CodeSectionLength = -1;
                _currentFormat.DataSectionLength = -1;
                return dataBase;
            }

            // Try to read as PE
            _inputFile.Seek(0, SeekOrigin.Begin);
            var pe = PortableExecutable.Create(_inputFile);
            if (pe != null)
            {
                _currentFormat.ExecutableType = ExecutableType.PE;
                _currentFormat.ExecutableOffset = pe.OverlayAddress;

                // Get the text section
                var section = pe.GetFirstSection(".text");
                if (section != null)
                    _currentFormat.CodeSectionLength = section.VirtualSize;

                // Get the data section
                section = pe.GetFirstSection(".data");
                if (section != null)
                    _currentFormat.DataSectionLength = section.VirtualSize;

                return dataBase;
            }

            return dataBase;
        }

        /// <summary>
        /// Close the possible Wise installer
        /// </summary>
        private void Close()
        {
            _inputFile?.Close();
        }
    }
}
