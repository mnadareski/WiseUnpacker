using System;
using System.IO;
using SabreTools.IO.Compression.Deflate;
using SabreTools.IO.Streams;
using SabreTools.Models.WiseInstaller;
using SabreTools.Models.WiseInstaller.Actions;

namespace SabreTools.Serialization.Wrappers
{
    public class WiseOverlayHeader : WrapperBase<OverlayHeader>
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Wise Installer Overlay Header";

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

                offset += 1; // DllNameLen
                if (Model.DllNameLen > 0)
                {
                    offset += Model.DllNameLen;
                    offset += 4; // DllSize
                }

                offset += 4; // Flags
                offset += 12; // GraphicsData
                offset += 4; // WiseScriptExitEventOffset
                offset += 4; // WiseScriptCancelEventOffset
                offset += 4; // WiseScriptInflatedSize
                offset += 4; // WiseScriptDeflatedSize
                offset += 4; // WiseDllDeflatedSize
                offset += 4; // Ctl3d32DeflatedSize
                offset += 4; // SomeData4DeflatedSize
                offset += 4; // RegToolDeflatedSize
                offset += 4; // ProgressDllDeflatedSize
                offset += 4; // SomeData7DeflatedSize
                offset += 4; // SomeData8DeflatedSize
                offset += 4; // SomeData9DeflatedSize
                offset += 4; // SomeData10DeflatedSize
                offset += 4; // FinalFileDeflatedSize
                offset += 4; // FinalFileInflatedSize
                offset += 4; // EOF

                if (DibDeflatedSize == 0 && Model.Endianness == 0)
                    return offset;

                offset += 4; // DibDeflatedSize
                offset += 4; // DibInflatedSize

                if (Model.InstallScriptDeflatedSize != null)
                    offset += 4; // InstallScriptDeflatedSize

                if (Model.CharacterSet != null)
                    offset += 4; // CharacterSet

                offset += 2; // Endianness
                offset += 1; // InitTextLen
                offset += Model.InitTextLen;

                return offset;
            }
        }

        /// <summary>
        /// Installer data offset
        /// </summary>
        /// <remarks>
        /// This is the offset marking the point after all of the
        /// header-defined files. It is only set during extraction
        /// and is not used otherwise. It is automatically set if
        /// <see cref="ExtractHeaderDefinedFiles"/> is called.
        /// </remarks>
        public long InstallerDataOffset { get; private set; }

        /// <inheritdoc cref="OverlayHeader.Ctl3d32DeflatedSize"/>
        public uint Ctl3d32DeflatedSize => Model.Ctl3d32DeflatedSize;

        /// <inheritdoc cref="OverlayHeader.DibDeflatedSize"/>
        public uint DibDeflatedSize => Model.DibDeflatedSize;

        /// <inheritdoc cref="OverlayHeader.DibInflatedSize"/>
        public uint DibInflatedSize => Model.DibInflatedSize;

        /// <inheritdoc cref="OverlayHeader.FinalFileDeflatedSize"/>
        public uint FinalFileDeflatedSize => Model.FinalFileDeflatedSize;

        /// <inheritdoc cref="OverlayHeader.FinalFileInflatedSize"/>
        public uint FinalFileInflatedSize => Model.FinalFileInflatedSize;

        /// <inheritdoc cref="OverlayHeader.Flags"/>
        public OverlayHeaderFlags Flags => Model.Flags;

        /// <inheritdoc cref="OverlayHeader.InstallScriptDeflatedSize"/>
        public uint InstallScriptDeflatedSize => Model.InstallScriptDeflatedSize ?? 0;

        /// <summary>
        /// Indicates if data is packed in PKZIP containers
        /// </summary>
        public bool IsPKZIP
        {
            get
            {
#if NET20 || NET35
                return (Flags & OverlayHeaderFlags.WISE_FLAG_PK_ZIP) != 0;
#else
                return Flags.HasFlag(OverlayHeaderFlags.WISE_FLAG_PK_ZIP);
#endif
            }
        }

        /// <inheritdoc cref="OverlayHeader.ProgressDllDeflatedSize"/>
        public uint ProgressDllDeflatedSize => Model.ProgressDllDeflatedSize;

        /// <inheritdoc cref="OverlayHeader.SomeData4DeflatedSize"/>
        public uint SomeData4DeflatedSize => Model.SomeData4DeflatedSize;

        /// <inheritdoc cref="OverlayHeader.SomeData7DeflatedSize"/>
        public uint SomeData7DeflatedSize => Model.SomeData7DeflatedSize;

        /// <inheritdoc cref="OverlayHeader.SomeData8DeflatedSize"/>
        public uint SomeData8DeflatedSize => Model.SomeData8DeflatedSize;

        /// <inheritdoc cref="OverlayHeader.SomeData9DeflatedSize"/>
        public uint SomeData9DeflatedSize => Model.SomeData9DeflatedSize;

        /// <inheritdoc cref="OverlayHeader.SomeData10DeflatedSize"/>
        public uint SomeData10DeflatedSize => Model.SomeData10DeflatedSize;

        /// <inheritdoc cref="OverlayHeader.RegToolDeflatedSize"/>
        public uint RegToolDeflatedSize => Model.RegToolDeflatedSize;

        /// <inheritdoc cref="OverlayHeader.WiseDllDeflatedSize"/>
        public uint WiseDllDeflatedSize => Model.WiseDllDeflatedSize;

        /// <inheritdoc cref="OverlayHeader.WiseScriptDeflatedSize"/>
        public uint WiseScriptDeflatedSize => Model.WiseScriptDeflatedSize;

        /// <inheritdoc cref="OverlayHeader.WiseScriptInflatedSize"/>
        public uint WiseScriptInflatedSize => Model.WiseScriptInflatedSize;

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public WiseOverlayHeader(OverlayHeader? model, byte[]? data, int offset)
            : base(model, data, offset)
        {
            // All logic is handled by the base class
        }

        /// <inheritdoc/>
        public WiseOverlayHeader(OverlayHeader? model, Stream? data)
            : base(model, data)
        {
            // All logic is handled by the base class
        }

        /// <summary>
        /// Create a Wise installer overlay header from a byte array and offset
        /// </summary>
        /// <param name="data">Byte array representing the header</param>
        /// <param name="offset">Offset within the array to parse</param>
        /// <returns>A Wise installer overlay header wrapper on success, null on failure</returns>
        public static WiseOverlayHeader? Create(byte[]? data, int offset)
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
        /// Create a Wise installer overlay header from a Stream
        /// </summary>
        /// <param name="data">Stream representing the header</param>
        /// <returns>A Wise installer overlay header wrapper on success, null on failure</returns>
        public static WiseOverlayHeader? Create(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long currentOffset = data.Position;

                var model = Deserializers.WiseOverlayHeader.DeserializeStream(data);
                if (model == null)
                    return null;

                data.Seek(currentOffset, SeekOrigin.Begin);
                return new WiseOverlayHeader(model, data);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Extraction

        /// <summary>
        /// Extract the predefined, static files defined in the header
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the files extracted successfully, false otherwise</returns>
        /// <remarks>On success, this sets <see cref="InstallerDataOffset"/></remarks>
        public bool ExtractHeaderDefinedFiles(string outputDirectory, bool includeDebug)
        {
            // Seek to the compressed data offset
            _dataSource.Seek(CompressedDataOffset, SeekOrigin.Begin);
            if (includeDebug) Console.WriteLine($"Beginning of header-defined files: {CompressedDataOffset}");

            // Extract WiseColors.dib, if it exists
            var expected = new DeflateInfo { InputSize = DibDeflatedSize, OutputSize = DibInflatedSize, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, "WiseColors.dib", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract WiseScript.bin
            expected = new DeflateInfo { InputSize = WiseScriptDeflatedSize, OutputSize = WiseScriptInflatedSize, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, "WiseScript.bin", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract WISE0001.DLL, if it exists
            expected = new DeflateInfo { InputSize = WiseDllDeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "WISE0001.DLL", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract CTL3D32.DLL, if it exists
            expected = new DeflateInfo { InputSize = Ctl3d32DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "CTL3D32.DLL", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE0004, if it exists
            expected = new DeflateInfo { InputSize = SomeData4DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "FILE0004", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract Ocxreg32.EXE, if it exists
            expected = new DeflateInfo { InputSize = RegToolDeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "Ocxreg32.EXE", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract PROGRESS.DLL, if it exists
            expected = new DeflateInfo { InputSize = ProgressDllDeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "PROGRESS.DLL", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE0007, if it exists
            expected = new DeflateInfo { InputSize = SomeData7DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "FILE0007", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE0008, if it exists
            expected = new DeflateInfo { InputSize = SomeData8DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "FILE0008", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE0009, if it exists
            expected = new DeflateInfo { InputSize = SomeData9DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "FILE0009", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE000A, if it exists
            expected = new DeflateInfo { InputSize = SomeData10DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "FILE000A", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract install script, if it exists
            expected = new DeflateInfo { InputSize = InstallScriptDeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "INSTALL_SCRIPT", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE000{n}.DAT, if it exists
            expected = new DeflateInfo { InputSize = FinalFileDeflatedSize, OutputSize = FinalFileInflatedSize, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(_dataSource, IsPKZIP ? null : "FILE00XX.DAT", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            InstallerDataOffset = _dataSource.Position;

            return true;
        }

        /// <summary>
        /// Attempt to extract a file defined by a file header
        /// </summary>
        /// <param name="obj">Deflate information</param>
        /// <param name="index">File index for automatic naming</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted successfully, false otherwise</returns>
        /// <remarks>Requires <see cref="InstallerDataOffset"/> to be set</remarks> 
        public ExtractionStatus ExtractFile(InstallFile obj, int index, string outputDirectory, bool includeDebug)
        {
            // Get expected values
            var expected = new DeflateInfo
            {
                InputSize = obj.DeflateEnd - obj.DeflateStart,
                OutputSize = obj.InflatedSize,
                Crc32 = obj.Crc32,
            };

            // Perform path replacements
            string filename = obj.DestinationPathname ?? $"WISE{index:X4}";
            filename = filename.Replace("%", string.Empty);
            _dataSource.Seek(InstallerDataOffset + obj.DeflateStart, SeekOrigin.Begin);
            return InflateWrapper.ExtractFile(_dataSource,
                filename,
                outputDirectory,
                expected,
                IsPKZIP,
                includeDebug);
        }

        /// <summary>
        /// Attempt to extract a file defined by a file header
        /// </summary>
        /// <param name="obj">Deflate information</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted successfully, false otherwise</returns>
        /// <remarks>Requires <see cref="InstallerDataOffset"/> to be set</remarks> 
        public ExtractionStatus ExtractFile(DisplayBillboard obj, string outputDirectory, bool includeDebug)
        {
            // Get the generated base name
            string baseName = $"CustomBillboardSet_{obj.Flags:X4}-{obj.Operand_2}-{obj.Operand_3}";

            // If there are no deflate objects
            if (obj.DeflateInfo == null)
            {
                if (includeDebug) Console.WriteLine($"Skipping {baseName} because the deflate object array is null!");
                return ExtractionStatus.FAIL;
            }

            // Loop through the values
            for (int i = 0; i < obj.DeflateInfo.Length; i++)
            {
                // Get the deflate info object
                var info = obj.DeflateInfo[i];

                // Get expected values
                var expected = new DeflateInfo
                {
                    InputSize = info.DeflateEnd - info.DeflateStart,
                    OutputSize = info.InflatedSize,
                    Crc32 = 0,
                };

                // Perform path replacements
                string filename = $"{baseName}{i:X4}";
                _dataSource.Seek(InstallerDataOffset + info.DeflateStart, SeekOrigin.Begin);
                _ = InflateWrapper.ExtractFile(_dataSource, filename, outputDirectory, expected, IsPKZIP, includeDebug);
            }

            // Always return good -- TODO: Fix this
            return ExtractionStatus.GOOD;
        }

        /// <summary>
        /// Attempt to extract a file defined by a file header
        /// </summary>
        /// <param name="obj">Deflate information</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted successfully, false otherwise</returns>
        /// <remarks>Requires <see cref="InstallerDataOffset"/> to be set</remarks> 
        public ExtractionStatus ExtractFile(CustomDialogSet obj, string outputDirectory, bool includeDebug)
        {
            // Get expected values
            var expected = new DeflateInfo
            {
                InputSize = obj.DeflateEnd - obj.DeflateStart,
                OutputSize = obj.InflatedSize,
                Crc32 = 0,
            };

            // Perform path replacements
            string filename = $"CustomDialogSet_{obj.DisplayVariable}-{obj.Name}";
            filename = filename.Replace("%", string.Empty);
            _dataSource.Seek(InstallerDataOffset + obj.DeflateStart, SeekOrigin.Begin);
            return InflateWrapper.ExtractFile(_dataSource, filename, outputDirectory, expected, IsPKZIP, includeDebug);
        }

        /// <summary>
        /// Open a potential WISE installer file and any additional files
        /// </summary>
        /// <param name="filename">Input filename or base name to read from</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file could be opened, false otherwise</returns>
        public static bool OpenFile(string filename, bool includeDebug, out ReadOnlyCompositeStream? stream)
        {
            // If the file exists as-is
            if (File.Exists(filename))
            {
                var fileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream = new ReadOnlyCompositeStream([fileStream]);

                // Debug statement
                if (includeDebug) Console.WriteLine($"File {filename} was found and opened");

                // Strip the extension and rebuild
                string? directory = Path.GetDirectoryName(filename);
                filename = Path.GetFileNameWithoutExtension(filename);
                if (directory != null)
                    filename = Path.Combine(directory, filename);
            }

            // If the base name was provided, try to open the associated exe
            else if (File.Exists($"{filename}.EXE"))
            {
                var fileStream = File.Open($"{filename}.EXE", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream = new ReadOnlyCompositeStream([fileStream]);

                // Debug statement
                if (includeDebug) Console.WriteLine($"File {filename}.EXE was found and opened");
            }
            else if (File.Exists($"{filename}.exe"))
            {
                var fileStream = File.Open($"{filename}.exe", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream = new ReadOnlyCompositeStream([fileStream]);

                // Debug statement
                if (includeDebug) Console.WriteLine($"File {filename}.exe was found and opened");
            }

            // Otherwise, the file cannot be opened
            else
            {
                stream = null;
                return false;
            }

            // Get the pattern for file naming
            string filePattern = string.Empty;
            bool longDigits = false;

            byte fileno = 0;
            bool foundStart = false;
            for (; fileno < 3; fileno++)
            {
                if (File.Exists($"{filename}.W0{fileno}"))
                {
                    foundStart = true;
                    filePattern = $"{filename}.W";
                    longDigits = false;
                    break;
                }
                else if (File.Exists($"{filename}.w0{fileno}"))
                {
                    foundStart = true;
                    filePattern = $"{filename}.w";
                    longDigits = false;
                    break;
                }
                else if (File.Exists($"{filename}.00{fileno}"))
                {
                    foundStart = true;
                    filePattern = $"{filename}.";
                    longDigits = true;
                    break;
                }
            }

            // If no starting part has been found
            if (!foundStart)
                return true;

            // Loop through and try to read all additional files
            for (; ; fileno++)
            {
                string nextPart = longDigits ? $"{filePattern}{fileno:D3}" : $"{filePattern}{fileno:D2}";
                if (!File.Exists(nextPart))
                {
                    if (includeDebug) Console.WriteLine($"Part {nextPart} was not found");
                    break;
                }

                // Debug statement
                if (includeDebug) Console.WriteLine($"Part {nextPart} was found and appended");

                var fileStream = File.Open(nextPart, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream.AddStream(fileStream);
            }

            return true;
        }

        #endregion
    }
}
