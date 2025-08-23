using System;
using System.IO;
using SabreTools.Hashing;
using SabreTools.IO.Compression.Deflate;
using SabreTools.IO.Extensions;
using SabreTools.Models.BFPK;
using SabreTools.Models.WiseInstaller;

namespace SabreTools.Serialization.Wrappers
{
    public class WiseSectionHeader : WrapperBase<SectionHeader>
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Self-Extracting Wise Installer Header";

        #endregion

        #region Extension Properties

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

        /// <inheritdoc cref="SectionHeader.StringValues"/>
        public byte[]? StringValues => Model.StringValues;
        
        /// <inheritdoc cref="SectionHeader.Strings"/>
        public byte[]? Strings => Model.Strings;

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
                var model = Deserializers.WiseSectionHeader.DeserializeStream(data);
                if (model == null)
                    return null;

                return new WiseSectionHeader(model, data);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Extraction

        /// <summary>
        /// Extract all files from a Wise Self-Extracting installer to an output directory
        /// </summary>
        /// <param name="data">Stream representing the Wise Self-Extracting installer .WISE section</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if all files extracted, false otherwise</returns>
        public static bool ExtractAll(Stream? data, string outputDirectory, bool includeDebug) =>
            ExtractAll(data, sourceDirectory: null, outputDirectory, includeDebug);

        /// <summary>
        /// Extract all files from a Wise Self-Extracting installer to an output directory
        /// </summary>
        /// <param name="data">Stream representing the Wise Self-Extracting installer .WISE section</param>
        /// <param name="sourceDirectory">Directory where installer files live, if possible</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if all files extracted, false otherwise</returns>
        public static bool ExtractAll(Stream? data, string? sourceDirectory, string outputDirectory, bool includeDebug)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return false;

            var header = Create(data);

            // Attempt to get the section header
            if (header == null)
            {
                if (includeDebug) Console.Error.WriteLine("Could not parse the section header");
                return false;
            }

            // Extract the header-defined files
            bool extracted = header.ExtractHeaderDefinedFiles(data, outputDirectory, includeDebug, out long dataStart);
            if (!extracted)
            {
                if (includeDebug) Console.Error.WriteLine("Could not extract header-defined files");
                return false;
            }

            // TODO: strings are whatever is between the dataStart and the end of the header. Hook this up after everything else is fixed.

            return true;
        }

        // Currently unaware of any NE samples. That said, as they wouldn't have a .WISE section, it's unclear how such
        // samples could be identified.

        /// <summary>
        /// Extract the predefined, static files defined in the header
        /// </summary>
        /// <param name="data">Stream representing the Wise Self-Extracting installer .WISE section</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the files extracted successfully, false otherwise</returns>
        private bool ExtractHeaderDefinedFiles(Stream data, string outputDirectory, bool includeDebug, out long dataStart)
        {
            // Determine where the remaining compressed data starts
            dataStart = data.Position;

            // This has to run back to front due to lacking any other known way to derive offsets

            // Does output size include the crc32? Doesn't seem to?

            // Comparing against extractionstatus GOOD for now.
            
            // Extract first executable, if it exists
            if (ExtractFile(data, "FirstExecutable.exe", outputDirectory, FirstExecutableFileEntryLength, includeDebug)
                != ExtractionStatus.GOOD)
                return false;

            // Extract second executable, if it exists
            if (ExtractFile(data, "SecondExecutable.exe", outputDirectory, SecondExecutableFileEntryLength, 
                includeDebug)
                != ExtractionStatus.GOOD)
                return false;
            
            // Extract second executable, if it exists
            if (ExtractFile(data, "ThirdExecutable.exe", outputDirectory, ThirdExecutableFileEntryLength, 
                    includeDebug)
                != ExtractionStatus.GOOD)
                return false;
            
            // Extract main MSI file
            if (ExtractFile(data, "ExtractedMsi.msi", outputDirectory, MsiFileEntryLength, includeDebug)
                != ExtractionStatus.GOOD)
                return false;
            
            return true;
        }

        /// <summary>
        /// Attempt to extract a file defined by a filename
        /// </summary>
        /// <param name="source">Stream representing the deflated data</param>
        /// <param name="filename">Output filename, null to auto-generate</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="entrySize">Expected size of the file plus crc32</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>Extraction status representing the final state</returns>
        /// <remarks>Assumes that the current stream position is the end of where the data lives</remarks>
        private ExtractionStatus ExtractFile(Stream source,
            string filename,
            string outputDirectory,
            uint entrySize,
            bool includeDebug)
        {
            // Debug output
            if (includeDebug) Console.WriteLine($"Attempting to extract {filename}");

            // Extract the file
            var destination = new MemoryStream();
            ExtractionStatus status;
            if (!(Version != null && Version[1] == 0x01))
            {
                status = ExtractStreamWithChecksum(source,
                    destination,
                    entrySize,
                    includeDebug);   
            }
            else // hack for Codesited5.exe , very early and very strange.
            {
                status = ExtractStreamWithoutChecksum(source,
                    destination,
                    entrySize,
                    includeDebug);
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
        /// <param name="source">Stream representing the source data</param>
        /// <param name="destination">Stream where the file data will be written</param>
        /// <param name="entrySize">Expected size of the file plus crc32</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns></returns>
        public static ExtractionStatus ExtractStreamWithChecksum(Stream source,
            Stream destination,
            uint entrySize,
            bool includeDebug)
        {
            // Debug output
            if (includeDebug) Console.WriteLine($"Offset: {source.Position:X8}, Expected Read: {entrySize}, Expected Write:{entrySize - 4}"); // clamp to zero
            
            //if (includeDebug) Console.WriteLine($"Offset: {source.Position:X8}, Expected Read: {entrySize}, Expected Write: {entrySize - 4}, Expected CRC-32: {expected.Crc32:X8}");    
            // Check the validity of the inputs
            if (entrySize == 0)
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read 0 bytes");
                return ExtractionStatus.GOOD; // If size is 0, then it shouldn't be extracted
            }
            else if (entrySize > (source.Length - source.Position))
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read {entrySize} bytes but only {source.Position} bytes remain");
                return ExtractionStatus.INVALID;
            }

            // Cache the current offset
            long current = source.Position;

            // Extract the file
            // TODO: read in blocks so you can hash as you read?
            try
            {
                byte[] actual = source.ReadBytes((int)entrySize - 4);
                using var hasher = new HashWrapper(HashType.CRC32);
                hasher.Process(actual, 0, actual.Length);
                hasher.Terminate();
                byte[] hashBytes = hasher.CurrentHashBytes!;
                uint actualCrc32 = BitConverter.ToUInt32(hashBytes, 0);
                uint expectedCrc32 = source.ReadUInt32();
                if (expectedCrc32 != actualCrc32)
                {
                    if (includeDebug) Console.Error.WriteLine($"Mismatched CRC-32 values!");
                    return ExtractionStatus.BAD_CRC;
                }
                // Debug output
                if (includeDebug) Console.WriteLine($"CRC-32: {actualCrc32:X8}");
                destination.Write(actual, 0, actual.Length);
                return ExtractionStatus.GOOD;
            }
            catch
            {
                // TODO: How to handle error handling?
                if (includeDebug) Console.Error.WriteLine($"Could not extract");
                return ExtractionStatus.FAIL;
            }
        }
                /// <summary>
        /// Extract source data without a trailing CRC-32 checksum
        /// </summary>
        /// <param name="source">Stream representing the source data</param>
        /// <param name="destination">Stream where the file data will be written</param>
        /// <param name="entrySize">Expected size of the file</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns></returns>
        public static ExtractionStatus ExtractStreamWithoutChecksum(Stream source,
            Stream destination,
            uint entrySize,
            bool includeDebug)
        {
            // Debug output
            if (includeDebug) Console.WriteLine($"Offset: {source.Position:X8}, Expected Read: {entrySize}, Expected Write:{entrySize - 4}");
            
            //if (includeDebug) Console.WriteLine($"Offset: {source.Position:X8}, Expected Read: {entrySize}, Expected Write: {entrySize - 4}, Expected CRC-32: {expected.Crc32:X8}");    
            // Check the validity of the inputs
            if (entrySize == 0)
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read 0 bytes");
                return ExtractionStatus.GOOD; // If size is 0, then it shouldn't be extracted
            }
            else if (entrySize > (source.Length - source.Position))
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read {entrySize} bytes but only {source.Position} bytes remain");
                return ExtractionStatus.INVALID;
            }

            // Cache the current offset
            long current = source.Position;

            // Extract the file
            // TODO: read in blocks so you can hash as you read?
            try
            {
                byte[] actual = source.ReadBytes((int)entrySize);
                // Debug output
                if (includeDebug) Console.WriteLine($"No CRC-32!");
                destination.Write(actual, 0, actual.Length);
                return ExtractionStatus.GOOD;
            }
            catch
            {
                // TODO: How to handle error handling?
                if (includeDebug) Console.Error.WriteLine($"Could not extract");
                return ExtractionStatus.FAIL;
            }
        }

        #endregion
    }
}
