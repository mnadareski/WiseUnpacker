using System;
using System.IO;
using SabreTools.Hashing;
using SabreTools.IO.Compression.Deflate;
using SabreTools.IO.Extensions;
using SabreTools.Models.WiseInstaller;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Wrappers
{
    public class WiseSectionHeader : WrapperBase<SectionHeader>, IExtractable
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Self-Extracting Wise Installer Header";

        #endregion

        #region Extension Properties

        /// <summary>
        /// Returns the offset relative to the start of the header
        /// where the compressed data lives
        /// </summary>
        public long CompressedDataOffset
        {
            get
            {
                long offset = 0;

                offset += 4; // UnknownDataSize
                offset += 4; // SecondExecutableFileEntryLength
                offset += 4; // UnknownValue2
                offset += 4; // UnknownValue3
                offset += 4; // UnknownValue4
                offset += 4; // FirstExecutableFileEntryLength
                offset += 4; // MsiFileEntryLength
                offset += 4; // UnknownValue7
                offset += 4; // UnknownValue8
                offset += 4; // ThirdExecutableFileEntryLength
                offset += 4; // UnknownValue10
                offset += 4; // UnknownValue11
                offset += 4; // UnknownValue12
                offset += 4; // UnknownValue13
                offset += 4; // UnknownValue14
                offset += 4; // UnknownValue15
                offset += 4; // UnknownValue16
                offset += 4; // UnknownValue17
                offset += 4; // UnknownValue18
                offset += Version?.Length ?? 0;
                offset += Model.TmpString == null ? 0 : Model.TmpString.Length + 1;
                offset += Model.GuidString == null ? 0 : Model.GuidString.Length + 1;
                offset += Model.NonWiseVersion == null ? 0 : Model.NonWiseVersion.Length + 1;
                offset += Model.PreFontValue == null ? 0 : Model.PreFontValue.Length;
                offset += 4; // FontSize
                offset += Model.PreStringValues == null ? 0 : Model.PreStringValues.Length;
                if (Model.Strings != null)
                {
                    foreach (var str in Model.Strings)
                    {
                        offset += str.Length;
                    }
                }

                return offset;
            }
        }

        /// <inheritdoc cref="SectionHeader.UnknownDataSize"/>
        public uint UnknownDataSize => Model.UnknownDataSize;

        /// <inheritdoc cref="SectionHeader.SecondExecutableFileEntryLength"/> // TODO: VERIFY ON CHANGE
        public uint SecondExecutableFileEntryLength => Model.SecondExecutableFileEntryLength;

        /// <inheritdoc cref="SectionHeader.UnknownValue2"/>
        public uint UnknownValue2 => Model.UnknownValue2;

        /// <inheritdoc cref="SectionHeader.UnknownValue3"/>
        public uint UnknownValue3 => Model.UnknownValue3;

        /// <inheritdoc cref="SectionHeader.UnknownValue4"/>
        public uint UnknownValue4 => Model.UnknownValue4;

        /// <inheritdoc cref="SectionHeader.FirstExecutableFileEntryLength"/>
        public uint FirstExecutableFileEntryLength => Model.FirstExecutableFileEntryLength; // TODO: VERIFY ON CHANGE

        /// <inheritdoc cref="SectionHeader.MsiFileEntryLength"/>
        public uint MsiFileEntryLength => Model.MsiFileEntryLength;

        /// <inheritdoc cref="SectionHeader.UnknownValue7"/>
        public uint UnknownValue7 => Model.UnknownValue7;

        /// <inheritdoc cref="SectionHeader.UnknownValue8"/>
        public uint UnknownValue8 => Model.UnknownValue8;

        /// <inheritdoc cref="SectionHeader.ThirdExecutableFileEntryLength"/>
        public uint ThirdExecutableFileEntryLength => Model.ThirdExecutableFileEntryLength;

        /// <inheritdoc cref="SectionHeader.UnknownValue10"/>
        public uint UnknownValue10 => Model.UnknownValue10;

        /// <inheritdoc cref="SectionHeader.UnknownValue11"/>
        public uint UnknownValue11 => Model.UnknownValue11;

        /// <inheritdoc cref="SectionHeader.UnknownValue12"/>
        public uint UnknownValue12 => Model.UnknownValue12;

        /// <inheritdoc cref="SectionHeader.UnknownValue13"/>
        public uint UnknownValue13 => Model.UnknownValue13;

        /// <inheritdoc cref="SectionHeader.UnknownValue14"/>
        public uint UnknownValue14 => Model.UnknownValue14;

        /// <inheritdoc cref="SectionHeader.UnknownValue15"/>
        public uint UnknownValue15 => Model.UnknownValue15;

        /// <inheritdoc cref="SectionHeader.UnknownValue16"/>
        public uint UnknownValue16 => Model.UnknownValue16;

        /// <inheritdoc cref="SectionHeader.UnknownValue17"/>
        public uint UnknownValue17 => Model.UnknownValue17;

        /// <inheritdoc cref="SectionHeader.UnknownValue18"/>
        public uint UnknownValue18 => Model.UnknownValue18;

        /// <inheritdoc cref="SectionHeader.Version"/>
        public byte[]? Version => Model.Version;

        /// <inheritdoc cref="SectionHeader.PreStringValues"/>
        public byte[]? PreStringValues => Model.PreStringValues;

        /// <inheritdoc cref="SectionHeader.Strings"/>
        public byte[][]? Strings => Model.Strings;

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public WiseSectionHeader(SectionHeader? model, byte[]? data, int offset)
            : base(model, data, offset)
        {
            // All logic is handled by the base class
        }

        /// <inheritdoc/>
        public WiseSectionHeader(SectionHeader? model, Stream? data)
            : base(model, data)
        {
            // All logic is handled by the base class
        }

        /// <summary>
        /// Create a Wise Self-Extracting installer .WISE section from a byte array and offset
        /// </summary>
        /// <param name="data">Byte array representing the section</param>
        /// <param name="offset">Offset within the array to parse</param>
        /// <returns>A Wise Self-Extracting installer .WISE section wrapper on success, null on failure</returns>
        public static WiseSectionHeader? Create(byte[]? data, int offset)
        {
            // If the data is invalid
            if (data == null || data.Length == 0)
                return null;

            // If the offset is out of bounds
            if (offset < 0 || offset >= data.Length)
                return null;

            // Create a memory stream and use that
            var dataStream = new MemoryStream(data, offset, data.Length - offset);
            return Create(dataStream);
        }

        /// <summary>
        /// Create a Wise Self-Extracting installer .WISE section from a Stream
        /// </summary>
        /// <param name="data">Stream representing the section</param>
        /// <returns>A Wise Self-Extracting installer .WISE section wrapper on success, null on failure</returns>
        public static WiseSectionHeader? Create(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long currentOffset = data.Position;

                var model = Deserializers.WiseSectionHeader.DeserializeStream(data);
                if (model == null)
                    return null;

                data.Seek(currentOffset, SeekOrigin.Begin);
                return new WiseSectionHeader(model, data);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Extraction

        /// <inheritdoc/>
        public bool Extract(string outputDirectory, bool includeDebug)
        {
            // Extract the header-defined files
            bool extracted = ExtractHeaderDefinedFiles(outputDirectory, includeDebug);
            if (!extracted)
            {
                if (includeDebug) Console.Error.WriteLine("Could not extract header-defined files");
                return false;
            }
            
            return true;
        }

        // Currently unaware of any NE samples. That said, as they wouldn't have a .WISE section, it's unclear how such
        // samples could be identified.

        /// <summary>
        /// Extract the predefined, static files defined in the header
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the files extracted successfully, false otherwise</returns>
        private bool ExtractHeaderDefinedFiles(string outputDirectory, bool includeDebug)
        {
            // Seek to the compressed data offset
            _dataSource.Seek(CompressedDataOffset, SeekOrigin.Begin);
            
            // Extract first executable, if it exists
            if (ExtractFile("FirstExecutable.exe", outputDirectory, FirstExecutableFileEntryLength, includeDebug) != ExtractionStatus.GOOD)
                return false;

            // Extract second executable, if it exists
            // If there's a size provided for the second executable but no size for the first executable, the size of
            // the second executable appears to be some unrelated value that's larger than the second executable
            // actually is. Currently unable to extract properly in these cases, as no header value in such installers
            // seems to actually correspond to the real size of the second executable.
            if (ExtractFile("SecondExecutable.exe", outputDirectory, SecondExecutableFileEntryLength, includeDebug) != ExtractionStatus.GOOD)
                return false;

            // Extract third executable, if it exists
            if (ExtractFile("ThirdExecutable.exe", outputDirectory, ThirdExecutableFileEntryLength, includeDebug) != ExtractionStatus.GOOD)
                return false;

            // Extract main MSI file
            if (ExtractFile("ExtoutputDirectory: ractedMsi.msi", outputDirectory, MsiFileEntryLength, includeDebug) != ExtractionStatus.GOOD)
            {
                // Fallback- seek to the position that's the length of the MSI file entry from the end, then try and
                // extract from there.
                _dataSource.Seek(-MsiFileEntryLength, SeekOrigin.End);
                if (ExtractFile("ExtractedMsi.msi", outputDirectory, MsiFileEntryLength, includeDebug) != ExtractionStatus.GOOD)
                    return false; // The fallback also failed.
            }

            return true;
        }

        /// <summary>
        /// Attempt to extract a file defined by a filename
        /// </summary>
        /// <param name="filename">Output filename, null to auto-generate</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="entrySize">Expected size of the file plus crc32</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>Extraction status representing the final state</returns>
        /// <remarks>Assumes that the current stream position is the end of where the data lives</remarks>
        private ExtractionStatus ExtractFile(string filename,
            string outputDirectory,
            uint entrySize,
            bool includeDebug)
        {
            if (includeDebug) Console.WriteLine($"Attempting to extract {filename}");

            // Extract the file
            var destination = new MemoryStream();
            ExtractionStatus status;
            if (!(Version != null && Version[1] == 0x01))
            {
                status = ExtractStreamWithChecksum(destination, entrySize, includeDebug);
            }
            else // hack for Codesited5.exe , very early and very strange.
            {
                status = ExtractStreamWithoutChecksum(destination, entrySize, includeDebug);
            }

            // If the extracted data is invalid
            if (status != ExtractionStatus.GOOD || destination == null)
                return status;

            // Ensure the full output directory exists
            filename = Path.Combine(outputDirectory, filename);
            var directoryName = Path.GetDirectoryName(filename);
            if (directoryName != null && !Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            // Write the output file
            File.WriteAllBytes(filename, destination.ToArray());
            return status;
        }

        /// <summary>
        /// Extract source data with a trailing CRC-32 checksum
        /// </summary>
        /// <param name="destination">Stream where the file data will be written</param>
        /// <param name="entrySize">Expected size of the file plus crc32</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns></returns>
        private ExtractionStatus ExtractStreamWithChecksum(Stream destination, uint entrySize, bool includeDebug)
        {
            // Debug output
            if (includeDebug) Console.WriteLine($"Offset: {_dataSource.Position:X8}, Expected Read: {entrySize}, Expected Write:{entrySize - 4}"); // clamp to zero

            // Check the validity of the inputs
            if (entrySize == 0)
            {
                if (includeDebug) Console.Error.WriteLine("Not attempting to extract, expected to read 0 bytes");
                return ExtractionStatus.GOOD; // If size is 0, then it shouldn't be extracted
            }
            else if (entrySize > (_dataSource.Length - _dataSource.Position))
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read {entrySize} bytes but only {_dataSource.Position} bytes remain");
                return ExtractionStatus.INVALID;
            }

            // Extract the file
            try
            {
                byte[] actual = _dataSource.ReadBytes((int)entrySize - 4);
                uint expectedCrc32 = _dataSource.ReadUInt32();

                // Debug output
                if (includeDebug) Console.WriteLine($"Expected CRC-32: {expectedCrc32:X8}");

                byte[]? hashBytes = HashTool.GetByteArrayHashArray(actual, HashType.CRC32);
                if (hashBytes != null)
                {
                    uint actualCrc32 = BitConverter.ToUInt32(hashBytes, 0);

                    // Debug output
                    if (includeDebug) Console.WriteLine($"Actual CRC-32: {actualCrc32:X8}");

                    if (expectedCrc32 != actualCrc32)
                    {
                        if (includeDebug) Console.Error.WriteLine("Mismatched CRC-32 values!");
                        return ExtractionStatus.BAD_CRC;
                    }
                }

                destination.Write(actual, 0, actual.Length);
                return ExtractionStatus.GOOD;
            }
            catch
            {
                if (includeDebug) Console.Error.WriteLine("Could not extract");
                return ExtractionStatus.FAIL;
            }
        }

        /// <summary>
        /// Extract source data without a trailing CRC-32 checksum
        /// </summary>
        /// <param name="destination">Stream where the file data will be written</param>
        /// <param name="entrySize">Expected size of the file</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns></returns>
        private ExtractionStatus ExtractStreamWithoutChecksum(Stream destination, uint entrySize, bool includeDebug)
        {
            // Debug output
            if (includeDebug) Console.WriteLine($"Offset: {_dataSource.Position:X8}, Expected Read: {entrySize}, Expected Write:{entrySize - 4}");

            // Check the validity of the inputs
            if (entrySize == 0)
            {
                if (includeDebug) Console.Error.WriteLine("Not attempting to extract, expected to read 0 bytes");
                return ExtractionStatus.GOOD; // If size is 0, then it shouldn't be extracted
            }
            else if (entrySize > (_dataSource.Length - _dataSource.Position))
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read {entrySize} bytes but only {_dataSource.Position} bytes remain");
                return ExtractionStatus.INVALID;
            }

            // Extract the file
            try
            {
                byte[] actual = _dataSource.ReadBytes((int)entrySize);

                // Debug output
                if (includeDebug) Console.WriteLine("No CRC-32!");

                destination.Write(actual, 0, actual.Length);
                return ExtractionStatus.GOOD;
            }
            catch
            {
                if (includeDebug) Console.Error.WriteLine("Could not extract");
                return ExtractionStatus.FAIL;
            }
        }

        #endregion
    }
}
