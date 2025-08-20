using System;
using System.IO;
using SabreTools.IO.Compression.Deflate;
using SabreTools.Models.WiseInstaller;

namespace SabreTools.Serialization.Wrappers
{
    public class WiseSection : WrapperBase<Section>
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Self-Extracting Wise Installer Header";

        #endregion

        #region Extension Properties

        /// <inheritdoc cref="SectionHeader.UnknownValue0"/>
        public uint UnknownValue0 => Model.Header?.UnknownValue0 ?? 0;

        /// <inheritdoc cref="SectionHeader.SecondExecutableFileEntryLength"/> // TODO: VERIFY ON CHANGE
        public uint SecondExecutableFileEntryLength => Model.Header?.SecondExecutableFileEntryLength ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue2"/>
        public uint UnknownValue2 => Model.Header?.UnknownValue2 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue3"/>
        public uint UnknownValue3 => Model.Header?.UnknownValue3 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue4"/>
        public uint UnknownValue4 => Model.Header?.UnknownValue4 ?? 0;

        /// <inheritdoc cref="SectionHeader.FirstExecutableFileEntryLength"/>
        public uint FirstExecutableFileEntryLength => Model.Header?.FirstExecutableFileEntryLength ?? 0; // TODO: VERIFY ON CHANGE

        /// <inheritdoc cref="SectionHeader.MsiFileEntryLength"/>
        public uint MsiFileEntryLength => Model.Header?.MsiFileEntryLength ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue7"/>
        public uint UnknownValue7 => Model.Header?.UnknownValue7 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue8"/>
        public uint UnknownValue8 => Model.Header?.UnknownValue8 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue9"/>
        public uint UnknownValue9 => Model.Header?.UnknownValue9 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue10"/>
        public uint UnknownValue10 => Model.Header?.UnknownValue10 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue11"/>
        public uint UnknownValue11 => Model.Header?.UnknownValue11 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue12"/>
        public uint UnknownValue12 => Model.Header?.UnknownValue12 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue13"/>
        public uint UnknownValue13 => Model.Header?.UnknownValue13 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue14"/>
        public uint UnknownValue14 => Model.Header?.UnknownValue14 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue15"/>
        public uint UnknownValue15 => Model.Header?.UnknownValue15 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue16"/>
        public uint UnknownValue16 => Model.Header?.UnknownValue16 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue17"/>
        public uint UnknownValue17 => Model.Header?.UnknownValue17 ?? 0;

        /// <inheritdoc cref="SectionHeader.UnknownValue18"/>
        public uint UnknownValue18 => Model.Header?.UnknownValue18 ?? 0;

        /// <inheritdoc cref="SectionHeader.Version"/>
        public byte[]? Version => Model.Header?.Version;

        /// <inheritdoc cref="SectionHeader.Strings"/>
        public string? Strings => Model.Strings;

        /// <inheritdoc cref="SectionHeader.Entries"/>
        public FileEntry[]? Entries => Model.Entries;

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public WiseSection(Section? model, byte[]? data, int offset)
            : base(model, data, offset)
        {
            // All logic is handled by the base class
        }

        /// <inheritdoc/>
        public WiseSection(Section? model, Stream? data)
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
