using System;
using System.IO;
using SabreTools.IO.Compression.Deflate;
using SabreTools.Models.WiseInstaller;

namespace SabreTools.Serialization.Wrappers
{
    public class WiseSection : WrapperBase<WiseSectionHeader>
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Self-Extracting Wise Installer Header";

        #endregion

        #region Extension Properties

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue0"/>
        public uint UnknownValue0 => Model.UnknownValue0;

        /// <inheritdoc cref="WiseSectionHeader.SecondExecutableFileEntryLength"/> // TODO: VERIFY ON CHANGE
        public uint SecondExecutableFileEntryLength => Model.SecondExecutableFileEntryLength;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue2"/>
        public uint UnknownValue2 => Model.UnknownValue2;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue3"/>
        public uint UnknownValue3 => Model.UnknownValue3;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue4"/>
        public uint UnknownValue4 => Model.UnknownValue4;

        /// <inheritdoc cref="WiseSectionHeader.FirstExecutableFileEntryLength"/>
        public uint FirstExecutableFileEntryLength => Model.FirstExecutableFileEntryLength;// TODO: VERIFY ON CHANGE

        /// <inheritdoc cref="WiseSectionHeader.MsiFileEntryLength"/>
        public uint MsiFileEntryLength => Model.MsiFileEntryLength;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue7"/>
        public uint UnknownValue7 => Model.UnknownValue7;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue8"/>
        public uint UnknownValue8 => Model.UnknownValue8;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue9"/>
        public uint UnknownValue9 => Model.UnknownValue9;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue10"/>
        public uint UnknownValue10 => Model.UnknownValue10;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue11"/>
        public uint UnknownValue11 => Model.UnknownValue11;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue12"/>
        public uint UnknownValue12 => Model.UnknownValue12;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue13"/>
        public uint UnknownValue13 => Model.UnknownValue13;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue14"/>
        public uint UnknownValue14 => Model.UnknownValue14;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue15"/>
        public uint UnknownValue15 => Model.UnknownValue15;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue16"/>
        public uint UnknownValue16 => Model.UnknownValue16;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue17"/>
        public uint UnknownValue17 => Model.UnknownValue17;

        /// <inheritdoc cref="WiseSectionHeader.UnknownValue18"/>
        public uint UnknownValue18 => Model.UnknownValue18;

        /// <inheritdoc cref="WiseSectionHeader.Version"/>
        public byte[]? Version => Model.Version;

        /// <inheritdoc cref="WiseSectionHeader.Strings"/>
        public string? Strings => Model.Strings;

        /// <inheritdoc cref="WiseSectionHeader.Entries"/>
        public WiseSectionFileEntry[]? Entries => Model.Entries;

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public WiseSection(WiseSectionHeader? model, byte[]? data, int offset)
            : base(model, data, offset)
        {
            // All logic is handled by the base class
        }

        /// <inheritdoc/>
        public WiseSection(WiseSectionHeader? model, Stream? data)
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
        public static WiseSection? Create(byte[]? data, int offset)
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
        public static WiseSection? Create(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                var model = Deserializers.WiseSection.DeserializeStream(data);
                if (model == null)
                    return null;

                return new WiseSection(model, data);
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

            data.Seek(data.Length - 1, 0);// do I need to subtract 1?

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

        #endregion
    }
}
