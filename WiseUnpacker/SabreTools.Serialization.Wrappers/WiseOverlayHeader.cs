using System;
using System.IO;
using System.Text;
using SabreTools.IO.Compression.Deflate;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Matching;
using SabreTools.Models.NewExecutable;
using SabreTools.Models.WiseInstaller;

namespace SabreTools.Serialization.Wrappers
{
    public class WiseOverlayHeader : WrapperBase<OverlayHeader>
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Wise Installer Overlay Header";

        #endregion

        #region Extension Properties

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
                var model = Deserializers.WiseOverlayHeader.DeserializeStream(data);
                if (model == null)
                    return null;

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
        /// Extract all files from a Wise installer to an output directory
        /// </summary>
        /// <param name="filename">Input filename to read from</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if all files extracted, false otherwise</returns>
        public static bool ExtractAll(string? filename, string outputDirectory, bool includeDebug)
        {
            // If the filename is invalid
            if (filename == null)
                return false;

            // If the file could not be opened
            if (!OpenFile(filename, includeDebug, out var stream))
                return false;

            // Get the source directory
            string? sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(filename));

            return ExtractAll(stream, sourceDirectory, outputDirectory, includeDebug);
        }

        /// <summary>
        /// Extract all files from a Wise installer to an output directory
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if all files extracted, false otherwise</returns>
        public static bool ExtractAll(Stream? data, string outputDirectory, bool includeDebug)
            => ExtractAll(data, sourceDirectory: null, outputDirectory, includeDebug);

        /// <summary>
        /// Extract all files from a Wise installer to an output directory
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="sourceDirectory">Directory where installer files live, if possible</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if all files extracted, false otherwise</returns>
        public static bool ExtractAll(Stream? data, string? sourceDirectory, string outputDirectory, bool includeDebug)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return false;

            // Attempt to get the overlay header
            if (!FindOverlayHeader(data, includeDebug, out var header) || header == null)
            {
                if (includeDebug) Console.Error.WriteLine("Could not parse the overlay header");
                return false;
            }

            // Extract the header-defined files
            bool extracted = header.ExtractHeaderDefinedFiles(data, outputDirectory, includeDebug, out long dataStart);
            if (!extracted)
            {
                if (includeDebug) Console.Error.WriteLine("Could not extract header-defined files");
                return false;
            }

            // Open the script file from the output directory
            var scriptStream = File.OpenRead(Path.Combine(outputDirectory, "WiseScript.bin"));
            var script = WiseScript.Create(scriptStream);
            if (script == null)
            {
                if (includeDebug) Console.Error.WriteLine("Could not parse WiseScript.bin");
                return false;
            }

            // Process the state machine
            return script.ProcessStateMachine(data,
                sourceDirectory,
                dataStart,
                outputDirectory,
                header.IsPKZIP,
                includeDebug);
        }

        /// <summary>
        /// Find the overlay header from the Wise installer, if possible
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="header">The found overlay header on success, null otherwise</param>
        /// <returns>True if the header was found and valid, false otherwise</returns>
        public static bool FindOverlayHeader(Stream data, bool includeDebug, out WiseOverlayHeader? header)
        {
            // Set the default header value
            header = null;

            // Attempt to deserialize the file as either NE or PE
            var wrapper = WrapperFactory2.CreateExecutableWrapper(data);
            if (wrapper is NewExecutable ne)
            {
                return FindOverlayHeader(data, ne, includeDebug, out header);
            }
            else if (wrapper is PortableExecutable pe)
            {
                return FindOverlayHeader(data, pe, includeDebug, out header);
            }
            else
            {
                if (includeDebug) Console.Error.WriteLine("Only NE and PE executables are supported");
                return false;
            }
        }

        /// <summary>
        /// Find the overlay header from a NE Wise installer, if possible
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="nex">Wrapper representing the NE</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="header">The found overlay header on success, null otherwise</param>
        /// <returns>True if the header was found and valid, false otherwise</returns>
        public static bool FindOverlayHeader(Stream data, NewExecutable nex, bool includeDebug, out WiseOverlayHeader? header)
        {
            // Set the default header value
            header = null;

            // Get the overlay offset
            long overlayOffset = GetOverlayAddress(data, nex);
            if (overlayOffset < 0 || overlayOffset >= data.Length)
            {
                if (includeDebug) Console.Error.WriteLine("Could not parse the overlay header");
                return false;
            }

            // Attempt to get the overlay header
            data.Seek(overlayOffset, SeekOrigin.Begin);
            header = Create(data);
            if (header != null)
                return true;

            // Align and loop to see if it can be found
            data.Seek(overlayOffset, SeekOrigin.Begin);
            data.AlignToBoundary(0x10);
            overlayOffset = data.Position;
            while (data.Position < data.Length)
            {
                data.Seek(overlayOffset, SeekOrigin.Begin);
                header = Create(data);
                if (header != null)
                    return true;

                overlayOffset += 0x10;
            }

            header = null;
            return false;
        }

        /// <summary>
        /// Find the overlay header from a PE Wise installer, if possible
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="pex">Wrapper representing the PE</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="header">The found overlay header on success, null otherwise</param>
        /// <returns>True if the header was found and valid, false otherwise</returns>
        public static bool FindOverlayHeader(Stream data, PortableExecutable pex, bool includeDebug, out WiseOverlayHeader? header)
            => FindOverlayHeader(data, pex, dataOffset: 0, includeDebug, out header);

        /// <summary>
        /// Find the overlay header from a PE Wise installer, if possible
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="pex">Wrapper representing the PE</param>
        /// <param name="dataOffset">Adjustment offset for all operations</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="header">The found overlay header on success, null otherwise</param>
        /// <returns>True if the header was found and valid, false otherwise</returns>
        public static bool FindOverlayHeader(Stream data, PortableExecutable pex, long dataOffset, bool includeDebug, out WiseOverlayHeader? header)
        {
            // Set the default header value
            header = null;

            // Get the overlay offset
            long overlayOffset = GetOverlayAddress(pex, dataOffset, out long endOfFile);

            // Attempt to get the overlay header
            if (overlayOffset >= 0 && overlayOffset < endOfFile)
            {
                data.Seek(overlayOffset, SeekOrigin.Begin);
                header = Create(data);
                if (header != null)
                    return true;
            }

            // Check section data
            foreach (var section in pex.Model.SectionTable ?? [])
            {
                string sectionName = Encoding.ASCII.GetString(section.Name ?? []).TrimEnd('\0');
                long sectionOffset = section.VirtualAddress.ConvertVirtualAddress(pex.Model.SectionTable);
                data.Seek(sectionOffset, SeekOrigin.Begin);

                header = Create(data);
                if (header != null)
                    return true;

                // Check after the resource table
                if (sectionName == ".rsrc")
                {
                    // Data immediately following
                    long afterResourceOffset = sectionOffset + section.SizeOfRawData;
                    data.Seek(afterResourceOffset, SeekOrigin.Begin);

                    header = Create(data);
                    if (header != null)
                        return true;

                    // Data following padding data
                    data.Seek(afterResourceOffset, SeekOrigin.Begin);
                    _ = data.ReadNullTerminatedAnsiString();

                    header = Create(data);
                    if (header != null)
                        return true;
                }
            }

            // If there are no resources
            if (pex.Model.OptionalHeader?.ResourceTable == null || pex.ResourceData == null)
                return false;

            // Get the resources that have an executable signature
            bool exeResources = false;
            foreach (var kvp in pex.ResourceData)
            {
                if (kvp.Value == null || kvp.Value is not byte[] ba)
                    continue;
                if (!ba.StartsWith(Models.MSDOS.Constants.SignatureBytes))
                    continue;

                exeResources = true;
                break;
            }

            // If there are no executable resources
            if (!exeResources)
            {
                if (includeDebug) Console.Error.WriteLine("Could not find the overlay header");
                return false;
            }

            // Get the raw resource table offset
            long resourceTableOffset = pex.Model.OptionalHeader.ResourceTable.VirtualAddress.ConvertVirtualAddress(pex.Model.SectionTable);
            if (resourceTableOffset <= 0)
            {
                if (includeDebug) Console.Error.WriteLine("Could not find the overlay header");
                return false;
            }

            // Search the resource table data for the offset
            long resourceOffset = -1;
            data.Seek(resourceTableOffset, SeekOrigin.Begin);
            while (data.Position < resourceTableOffset + pex.Model.OptionalHeader.ResourceTable.Size && data.Position < data.Length)
            {
                ushort possibleSignature = data.ReadUInt16();
                if (possibleSignature == Models.MSDOS.Constants.SignatureUInt16)
                {
                    resourceOffset = data.Position - 2;
                    break;
                }

                data.Seek(-1, SeekOrigin.Current);
            }

            // If there was no valid offset, somehow
            if (resourceOffset == -1)
            {
                if (includeDebug) Console.Error.WriteLine("Could not find the overlay header");
                return false;
            }

            // Parse the executable and recurse
            data.Seek(resourceOffset, SeekOrigin.Begin);
            var resourceExe = WrapperFactory2.CreateExecutableWrapper(data);
            if (resourceExe is not PortableExecutable resourcePex)
            {
                if (includeDebug) Console.Error.WriteLine("Could not find the overlay header");
                return false;
            }

            return FindOverlayHeader(data, resourcePex, resourceOffset, includeDebug, out header);
        }

        /// <summary>
        /// Open a potential WISE installer file and any additional files
        /// </summary>
        /// <param name="filename">Input filename or base name to read from</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file could be opened, false otherwise</returns>
        private static bool OpenFile(string filename, bool includeDebug, out ReadOnlyCompositeStream? stream)
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

        /// <summary>
        /// Extract the predefined, static files defined in the header
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the files extracted successfully, false otherwise</returns>
        private bool ExtractHeaderDefinedFiles(Stream data, string outputDirectory, bool includeDebug, out long dataStart)
        {
            // Determine where the remaining compressed data starts
            dataStart = data.Position;

            // Extract WiseColors.dib, if it exists
            var expected = new DeflateInfo { InputSize = DibDeflatedSize, OutputSize = DibInflatedSize, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "WiseColors.dib", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract WiseScript.bin
            expected = new DeflateInfo { InputSize = WiseScriptDeflatedSize, OutputSize = WiseScriptInflatedSize, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "WiseScript.bin", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract WISE0001.DLL, if it exists
            expected = new DeflateInfo { InputSize = WiseDllDeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "WISE0001.DLL", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract CTL3D32.DLL, if it exists
            expected = new DeflateInfo { InputSize = Ctl3d32DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "CTL3D32.DLL", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE0004, if it exists
            expected = new DeflateInfo { InputSize = SomeData4DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "FILE0004", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract Ocxreg32.EXE, if it exists
            expected = new DeflateInfo { InputSize = RegToolDeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "Ocxreg32.EXE", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract PROGRESS.DLL, if it exists
            expected = new DeflateInfo { InputSize = ProgressDllDeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "PROGRESS.DLL", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE0007, if it exists
            expected = new DeflateInfo { InputSize = SomeData7DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "FILE0007", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE0008, if it exists
            expected = new DeflateInfo { InputSize = SomeData8DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "FILE0008", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE0009, if it exists
            expected = new DeflateInfo { InputSize = SomeData9DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "FILE0009", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE000A, if it exists
            expected = new DeflateInfo { InputSize = SomeData10DeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "FILE000A", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract install script, if it exists
            expected = new DeflateInfo { InputSize = InstallScriptDeflatedSize, OutputSize = -1, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, "INSTALL_SCRIPT", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            // Extract FILE000{n}.DAT, if it exists
            expected = new DeflateInfo { InputSize = FinalFileDeflatedSize, OutputSize = FinalFileInflatedSize, Crc32 = 0 };
            if (InflateWrapper.ExtractFile(data, IsPKZIP ? null : "FILE00XX.DAT", outputDirectory, expected, IsPKZIP, includeDebug) == ExtractionStatus.FAIL)
                return false;

            dataStart = data.Position;
            return true;
        }

        #endregion

        #region Serialization -- Move to Serialization once Models is updated

        /// <summary>
        /// Address of the overlay, if it exists
        /// </summary>
        /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/exefile.c"/>
        private static long GetOverlayAddress(Stream data, NewExecutable nex)
        {
            // Get the available source length, if possible
            long dataLength = nex.Length;
            if (dataLength == -1)
                return -1;

            // If a required property is missing
            if (nex.Model.Header == null || nex.Model.SegmentTable == null || nex.Model.ResourceTable?.ResourceTypes == null)
                return -1;

            // Search through the segments table to find the furthest
            long endOfSectionData = -1;
            foreach (var entry in nex.Model.SegmentTable)
            {
                // Get end of segment data
                long offset = entry.Offset * (1 << nex.Model.Header.SegmentAlignmentShiftCount) + entry.Length;

                // Read and find the end of the relocation data
                if ((entry.FlagWord & SegmentTableEntryFlag.RELOCINFO) != 0)
                {
                    // TODO: This should be unnecessary once this lives in Serialization
                    data.Seek(offset, SeekOrigin.Begin);
                    var relocationData = ParsePerSegmentData(data);

                    offset = data.Position;
                }

                if (offset > endOfSectionData)
                    endOfSectionData = offset;
            }

            // Search through the resources table to find the furthest
            foreach (var entry in nex.Model.ResourceTable.ResourceTypes)
            {
                // Skip invalid entries
                if (entry.ResourceCount == 0 || entry.Resources == null || entry.Resources.Length == 0)
                    continue;

                foreach (var resource in entry.Resources)
                {
                    int offset = (resource.Offset << nex.Model.ResourceTable.AlignmentShiftCount) + resource.Length;
                    if (offset > endOfSectionData)
                        endOfSectionData = offset;
                }
            }

            // If we didn't find the end of section data
            if (endOfSectionData <= 0)
                return -1;

            // Adjust the position of the data by 738 bytes
            // TODO: Investigate what the byte data is
            endOfSectionData += 738;

            // Cache and return the position
            return endOfSectionData;
        }

        /// <summary>
        /// Address of the overlay, if it exists
        /// </summary>
        private static long GetOverlayAddress(PortableExecutable pex, long dataOffset, out long dataLength)
        {
            // Get the available source length, if possible
            dataLength = pex.Length;
            if (dataLength == -1)
                return -1;

            // If the section table is missing
            if (pex.Model.SectionTable == null)
                return -1;

            // If we have certificate data, use that as the end
            if (pex.Model.OptionalHeader?.CertificateTable != null)
            {
                int certificateTableAddress = (int)pex.Model.OptionalHeader.CertificateTable.VirtualAddress.ConvertVirtualAddress(pex.Model.SectionTable);
                if (certificateTableAddress != 0 && dataOffset + certificateTableAddress < dataLength)
                    dataLength = dataOffset + certificateTableAddress;
            }

            // Search through all sections and find the furthest a section goes
            long overlayOffset = -1;
            foreach (var section in pex.Model.SectionTable)
            {
                // If we have an invalid section
                if (section == null)
                    continue;

                // If we have an invalid section address
                int sectionAddress = (int)section.VirtualAddress.ConvertVirtualAddress(pex.Model.SectionTable);
                if (sectionAddress == 0)
                    continue;

                // If we have an invalid section size
                if (section.SizeOfRawData == 0 && section.VirtualSize == 0)
                    continue;

                // Get the real section size
                int sectionSize;
                if (section.SizeOfRawData < section.VirtualSize)
                    sectionSize = (int)section.VirtualSize;
                else
                    sectionSize = (int)section.SizeOfRawData;

                // Compare and set the end of section data
                if (dataOffset + sectionAddress + sectionSize > overlayOffset)
                    overlayOffset = dataOffset + sectionAddress + sectionSize;
            }

            return overlayOffset;
        }

        /// <summary>
        /// Parse a Stream into an PerSegmentData
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled PerSegmentData on success, null on error</returns>
        private static PerSegmentData ParsePerSegmentData(Stream data)
        {
            var obj = new PerSegmentData();

            obj.RelocationRecordCount = data.ReadUInt16LittleEndian();
            obj.RelocationRecords = new RelocationRecord[obj.RelocationRecordCount];
            for (int i = 0; i < obj.RelocationRecords.Length; i++)
            {
                obj.RelocationRecords[i] = ParseRelocationRecord(data);
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an RelocationRecord
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled RelocationRecord on success, null on error</returns>
        private static RelocationRecord ParseRelocationRecord(Stream data)
        {
            var obj = new RelocationRecord();

            obj.SourceType = (RelocationRecordSourceType)data.ReadByteValue();
            obj.Flags = (RelocationRecordFlag)data.ReadByteValue();
            obj.Offset = data.ReadUInt16LittleEndian();

            switch (obj.Flags & RelocationRecordFlag.TARGET_MASK)
            {
                case RelocationRecordFlag.INTERNALREF:
                    obj.InternalRefRelocationRecord = ParseInternalRefRelocationRecord(data);
                    break;
                case RelocationRecordFlag.IMPORTORDINAL:
                    obj.ImportOrdinalRelocationRecord = ParseImportOrdinalRelocationRecord(data);
                    break;
                case RelocationRecordFlag.IMPORTNAME:
                    obj.ImportNameRelocationRecord = ParseImportNameRelocationRecord(data);
                    break;
                case RelocationRecordFlag.OSFIXUP:
                    obj.OSFixupRelocationRecord = ParseOSFixupRelocationRecord(data);
                    break;
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an InternalRefRelocationRecord
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled InternalRefRelocationRecord on success, null on error</returns>
        private static InternalRefRelocationRecord ParseInternalRefRelocationRecord(Stream data)
        {
            var obj = new InternalRefRelocationRecord();

            obj.SegmentNumber = data.ReadByteValue();
            obj.Reserved = data.ReadByteValue();
            obj.Offset = data.ReadUInt16LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an ImportOrdinalRelocationRecord
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ImportOrdinalRelocationRecord on success, null on error</returns>
        private static ImportOrdinalRelocationRecord ParseImportOrdinalRelocationRecord(Stream data)
        {
            var obj = new ImportOrdinalRelocationRecord();

            obj.Index = data.ReadUInt16LittleEndian();
            obj.Ordinal = data.ReadUInt16LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an ImportNameRelocationRecord
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ImportNameRelocationRecord on success, null on error</returns>
        private static ImportNameRelocationRecord ParseImportNameRelocationRecord(Stream data)
        {
            var obj = new ImportNameRelocationRecord();

            obj.Index = data.ReadUInt16LittleEndian();
            obj.Offset = data.ReadUInt16LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an OSFixupRelocationRecord
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled OSFixupRelocationRecord on success, null on error</returns>
        private static OSFixupRelocationRecord ParseOSFixupRelocationRecord(Stream data)
        {
            var obj = new OSFixupRelocationRecord();

            obj.FixupType = (OSFixupType)data.ReadUInt16LittleEndian();
            obj.Reserved = data.ReadUInt16LittleEndian();

            return obj;
        }

        #endregion
    }
}
