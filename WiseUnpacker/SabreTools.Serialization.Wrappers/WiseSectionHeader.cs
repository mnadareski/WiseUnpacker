using System;
using System.IO;
using SabreTools.IO.Compression.Deflate;
using SabreTools.IO.Extensions;
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

        /// <inheritdoc cref="SectionHeader.UnknownValue0"/>
        public uint UnknownValue0 => Model.UnknownValue0;

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

        /// <inheritdoc cref="SectionHeader.UnknownValue9"/>
        public uint UnknownValue9 => Model.UnknownValue9;

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

        /// <inheritdoc cref="SectionHeader.Strings"/>
        public string? Strings => Model.Strings;

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

            // Seeks to end of section to extract back to front
            data.Seek(data.Length - 1, 0); // do I need to subtract 1?

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

            // Extract main MSI file
            var expected = new DeflateInfo
            {
                InputSize = MsiFileEntryLength,
                OutputSize = MsiFileEntryLength - 4,
                Crc32 = 0
            };
            data.Seek(data.Position - MsiFileEntryLength, 0);
            if (InflateWrapper.ExtractFile(data, "ExtractedMsi.msi", outputDirectory, expected, false, includeDebug)
                == ExtractionStatus.FAIL)
                return false;
            data.Seek(data.Position - MsiFileEntryLength, 0);

            // Extract second executable, if it exists
            expected = new DeflateInfo
            {
                InputSize = SecondExecutableFileEntryLength,
                OutputSize =
                    SecondExecutableFileEntryLength - 4,
                Crc32 = 0
            };
            data.Seek(data.Position - SecondExecutableFileEntryLength, 0);
            if (InflateWrapper.ExtractFile(data, "WiseScript.bin", outputDirectory, expected, false, includeDebug)
                == ExtractionStatus.FAIL)
                return false;
            data.Seek(data.Position - SecondExecutableFileEntryLength, 0);

            // Extract first executable, if it exists
            expected = new DeflateInfo
            {
                InputSize = FirstExecutableFileEntryLength,
                OutputSize =
                    FirstExecutableFileEntryLength - 4,
                Crc32 = 0
            };
            data.Seek(data.Position - FirstExecutableFileEntryLength, 0);
            if (InflateWrapper.ExtractFile(data, "WISE0001.DLL", outputDirectory, expected, false, includeDebug)
                == ExtractionStatus.FAIL)
                return false;
            data.Seek(data.Position - FirstExecutableFileEntryLength, 0);

            dataStart = data.Position;
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
            ExtractionStatus status = ExtractStream(source,
                destination,
                entrySize,
                includeDebug);

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
        /// <param name="source">Stream representing the deflated data</param>
        /// <param name="destination">Stream where the inflated data will be written</param>
        /// <param name="entrySize">Expected size of the file plus crc32</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns></returns>
        public static ExtractionStatus ExtractStreamWithChecksum(Stream source,
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
                return ExtractionStatus.INVALID;
            }
            else if (entrySize > (source.Position)) // TODO: include header plus string length
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read {entrySize} bytes but only {source.Position} bytes remain");
                return ExtractionStatus.INVALID;
            }

            // Cache the current offset
            long current = source.Position;

            // Extract the file
            var actual = Inflate(source, destination);
            if (actual == null)
            {
                if (includeDebug) Console.Error.WriteLine($"Could not extract");
                return ExtractionStatus.FAIL;
            }

            // Seek to the true end of the data
            source.Seek(current + actual.InputSize, SeekOrigin.Begin);

            // If the read value is off-by-one after checksum
            if (actual.InputSize == expected.InputSize - 5)
            {
                // If not at the end of the file, get the corrected offset
                if (source.Position + 5 < source.Length)
                {
                    // TODO: What does this byte represent?
                    byte padding = source.ReadByteValue();
                    actual.InputSize += 1;

                    // Debug output
                    if (includeDebug) Console.WriteLine($"Off-by-one padding byte detected: 0x{padding:X2}");
                }
                else
                {
                    // Debug output
                    if (includeDebug) Console.WriteLine($"Not enough data to adjust offset");
                }
            }

            // If there is enough data to read the full CRC
            uint deflateCrc;
            if (source.Position + 4 < source.Length)
            {
                deflateCrc = source.ReadUInt32LittleEndian();
                actual.InputSize += 4;
            }
            // Otherwise, read what is possible and pad with 0x00
            else
            {
                byte[] deflateCrcBytes = new byte[4];
                int realCrcLength = source.Read(deflateCrcBytes, 0, (int)(source.Length - source.Position));

                // Parse as a little-endian 32-bit value
                deflateCrc = (uint)(deflateCrcBytes[0]
                            | (deflateCrcBytes[1] << 8)
                            | (deflateCrcBytes[2] << 16)
                            | (deflateCrcBytes[3] << 24));

                actual.InputSize += realCrcLength;
            }

            // If the CRC to check isn't set
            if (expected.Crc32 == 0)
                expected.Crc32 = deflateCrc;

            // Debug output
            if (includeDebug) Console.WriteLine($"DeflateStream CRC-32: {deflateCrc:X8}");

            // Verify the extracted data
            return VerifyExtractedData(source, current, expected, actual, includeDebug);
        }

        #endregion
    }
}
