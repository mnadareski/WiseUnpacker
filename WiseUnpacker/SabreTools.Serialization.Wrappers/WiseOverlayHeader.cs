using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SabreTools.Hashing;
using SabreTools.IO.Compression.Deflate;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Matching;
using SabreTools.Models.WiseInstaller;
using SabreTools.Models.WiseInstaller.Actions;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Wrappers
{
    public class WiseOverlayHeader : WrapperBase<OverlayHeader>
    {
        #region Enums

        /// <summary>
        /// Represents the status returned from extracting a file
        /// </summary>
        public enum ExtractStatus
        {
            /// <summary>
            /// Extraction wasn't performed because the inputs were invalid
            /// </summary>
            INVALID,

            /// <summary>
            /// No issues with the extraction
            /// </summary>
            GOOD,

            /// <summary>
            /// File extracted but was the wrong size
            /// </summary>
            /// <remarks>Rewinds the stream and deletes the bad file</remarks>
            WRONG_SIZE,

            /// <summary>
            /// File extracted but had the wrong CRC-32 value
            /// </summary>
            BAD_CRC,

            /// <summary>
            /// Extraction failed entirely
            /// </summary>
            FAIL,
        }

        #endregion

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
                var mkb = Deserializers.WiseOverlayHeader.DeserializeStream(data);
                if (mkb == null)
                    return null;

                return new WiseOverlayHeader(mkb, data);
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
        public static bool ExtractAll(Stream? data,
            string? sourceDirectory,
            string outputDirectory,
            bool includeDebug)
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
            return header.ProcessStateMachine(data,
                sourceDirectory,
                script,
                dataStart,
                outputDirectory,
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
            var wrapper = CreateExecutableWrapper(data);
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
            long overlayOffset = GetOverlayAddress(nex);
            if (overlayOffset < 0 || overlayOffset >= data.Length)
            {
                if (includeDebug) Console.Error.WriteLine("Could not parse the overlay header");
                return false;
            }

            // Attempt to get the overlay header
            data.Seek(overlayOffset, SeekOrigin.Begin);
            header = Create(data);
            return header != null;
        }

        /// <summary>
        /// Find the overlay header from a PE Wise installer, if possible
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="nex">Wrapper representing the PE</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="header">The found overlay header on success, null otherwise</param>
        /// <returns>True if the header was found and valid, false otherwise</returns>
        public static bool FindOverlayHeader(Stream data, PortableExecutable pex, bool includeDebug, out WiseOverlayHeader? header)
        {
            // Set the default header value
            header = null;

            // Get the overlay offset
            long overlayOffset = pex.OverlayAddress;

            // Attempt to get the overlay header
            if (overlayOffset >= 0 && overlayOffset < data.Length)
            {
                data.Seek(overlayOffset, SeekOrigin.Begin);
                header = Create(data);
                if (header != null)
                    return true;
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
            var resourceExe = CreateExecutableWrapper(data);
            if (resourceExe is not PortableExecutable resourcePex)
            {
                if (includeDebug) Console.Error.WriteLine("Could not find the overlay header");
                return false;
            }

            // Get the end of the file, if possible
            long endOfFile = resourcePex.GetEndOfFile();
            if (endOfFile == -1)
                return false;

            // If the section table is missing
            if (resourcePex.Model.SectionTable == null)
                return false;

            // If we have certificate data, use that as the end
            if (resourcePex.Model.OptionalHeader?.CertificateTable != null)
            {
                int certificateTableAddress = (int)resourcePex.Model.OptionalHeader.CertificateTable.VirtualAddress.ConvertVirtualAddress(resourcePex.Model.SectionTable);
                if (certificateTableAddress != 0 && resourceOffset + certificateTableAddress < endOfFile)
                    endOfFile = resourceOffset + certificateTableAddress;
            }

            // Search through all sections and find the furthest a section goes
            overlayOffset = -1;
            foreach (var section in resourcePex.Model.SectionTable)
            {
                // If we have an invalid section
                if (section == null)
                    continue;

                // If we have an invalid section address
                int sectionAddress = (int)section.VirtualAddress.ConvertVirtualAddress(resourcePex.Model.SectionTable);
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
                if (resourceOffset + sectionAddress + sectionSize > overlayOffset)
                    overlayOffset = resourceOffset + sectionAddress + sectionSize;
            }

            // If we didn't find the end of section data
            if (overlayOffset < 0 || overlayOffset >= endOfFile)
            {
                if (includeDebug) Console.Error.WriteLine($"Invalid overlay offset: {overlayOffset}");
                return false;
            }

            // Attempt to get the overlay header
            data.Seek(overlayOffset, SeekOrigin.Begin);
            header = Create(data);
            return header != null;
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
                string nextPart = longDigits ? $"{filePattern}{fileno:X3}" : $"{filePattern}{fileno:X2}";
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
        /// Create an instance of a wrapper based on the executable type
        /// </summary>
        /// <param name="stream">Stream data to parse</param>
        /// <returns>IWrapper representing the executable, null on error</returns>
        /// <remarks>Adapted from SabreTools.Serialization.Wrappers.WrapperFactory</remarks>
        private static IWrapper? CreateExecutableWrapper(Stream stream)
        {
            // Cache the current position
            long current = stream.Position;

            // Try to get an MS-DOS wrapper first
            var wrapper = MSDOS.Create(stream);
            if (wrapper == null || wrapper is not MSDOS msdos)
                return null;

            // Check for a valid new executable address
            if (msdos.Model.Header?.NewExeHeaderAddr == null || current + msdos.Model.Header.NewExeHeaderAddr >= stream.Length)
                return wrapper;

            // Try to read the executable info
            stream.Seek(current + msdos.Model.Header.NewExeHeaderAddr, SeekOrigin.Begin);
            var magic = stream.ReadBytes(4);

            // If we didn't get valid data at the offset
            if (magic == null)
            {
                return wrapper;
            }

            // New Executable
            else if (magic.StartsWith(Models.NewExecutable.Constants.SignatureBytes))
            {
                stream.Seek(current, SeekOrigin.Begin);
                return NewExecutable.Create(stream);
            }

            // Portable Executable
            else if (magic.StartsWith(Models.PortableExecutable.Constants.SignatureBytes))
            {
                stream.Seek(current, SeekOrigin.Begin);
                return PortableExecutable.Create(stream);
            }

            // Everything else fails
            return null;
        }

        /// <summary>
        /// Attempt to extract a file defined by a filename
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="filename">Output filename, null to auto-generate</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="expectedBytesRead">Expected number of bytes to read during inflation, -1 to ignore</param>
        /// <param name="expectedBytesWritten">Expected number of bytes to written during inflation, -1 to ignore</param>
        /// <param name="expectedCrc">Expected CRC-32 of the output file, 0 to ignore</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>Extraction status representing the final state</returns>
        /// <remarks>Assumes that the current stream position is where the compressed data lives</remarks>
        private ExtractStatus ExtractFile(Stream data,
            string? filename,
            string outputDirectory,
            long expectedBytesRead,
            long expectedBytesWritten,
            uint expectedCrc,
            bool includeDebug)
        {
            // Ensure directory separators are consistent
            filename ??= "[NULL]";
            if (Path.DirectorySeparatorChar == '\\')
                filename = filename.Replace('/', '\\');
            else if (Path.DirectorySeparatorChar == '/')
                filename = filename.Replace('\\', '/');

            // Ensure the full output directory exists
            filename = Path.Combine(outputDirectory, filename);
            var directoryName = Path.GetDirectoryName(filename);
            if (directoryName != null && !Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            // Extract the file
            ExtractStatus status = ExtractStream(data, ref filename, expectedBytesRead, expectedBytesWritten, expectedCrc, includeDebug, out var extracted);
            if (extracted != null)
                File.WriteAllBytes(filename, extracted.ToArray());

            return status;
        }

        /// <summary>
        /// Attempt to extract a file to a stream
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="filename">Output filename, if one exists</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="expectedBytesRead">Expected number of bytes to read during inflation, -1 to ignore</param>
        /// <param name="expectedBytesWritten">Expected number of bytes to written during inflation, -1 to ignore</param>
        /// <param name="expectedCrc">Expected CRC-32 of the output file, 0 to ignore</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="extracted">Output stream representing the extracted data, null on error</param>
        /// <returns>Extraction status representing the final state</returns>
        /// <remarks>Assumes that the current stream position is where the compressed data lives</remarks>
        public ExtractStatus ExtractStream(Stream data,
            ref string filename,
            long expectedBytesRead,
            long expectedBytesWritten,
            uint expectedCrc,
            bool includeDebug,
            out MemoryStream? extracted)
        {
            // Debug output
            if (includeDebug) Console.WriteLine($"Offset: {data.Position:X8}, Filename: {filename}, Expected Read: {expectedBytesRead}, Expected Write: {expectedBytesWritten}, Expected CRC-32: {expectedCrc:X8}");

            // Check the validity of the inputs
            if (expectedBytesRead == 0)
            {
                extracted = null;
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract {filename}, expected to read 0 bytes");
                return ExtractStatus.INVALID;
            }
            else if (expectedBytesRead > (data.Length - data.Position))
            {
                extracted = null;
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract {filename}, expected to read {expectedBytesRead} bytes but only {data.Length - data.Position} bytes remain");
                return ExtractStatus.INVALID;
            }

            // Cache the current offset
            long current = data.Position;

            // Skip the PKZIP header, if it exists
            Models.PKZIP.LocalFileHeader? zipHeader = null;
            long zipHeaderBytes = 0;
            if (IsPKZIP)
            {
                zipHeader = Deserializers.PKZIP.ParseLocalFileHeader(data);
                zipHeaderBytes = data.Position - current;

                // Always trust the PKZIP CRC-32 value over what is supplied
                if (zipHeader != null)
                    expectedCrc = zipHeader.CRC32;

                // If the filename is [NULL], replace with the zip filename
                if (zipHeader?.FileName != null)
                {
                    filename = filename.Replace("[NULL]", zipHeader.FileName);
                    if (includeDebug) Console.WriteLine($"Replaced [NULL] with {zipHeader.FileName}");
                }

                // Debug output
                if (includeDebug) Console.WriteLine($"PKZIP Filename: {zipHeader?.FileName}, PKZIP Expected Read: {zipHeader?.CompressedSize}, PKZIP Expected Write: {zipHeader?.UncompressedSize}, PKZIP Expected CRC-32: {zipHeader?.CRC32:X4}");
            }

            // Set the name from the zip header if missing
            filename ??= zipHeader?.FileName ?? Guid.NewGuid().ToString();

            // Extract the file
            extracted = new MemoryStream();
            if (!Inflate(data, extracted, out long bytesRead, out long bytesWritten, out uint writtenCrc))
            {
                if (includeDebug) Console.Error.WriteLine($"Could not extract {filename}");
                return ExtractStatus.FAIL;
            }

            // If not PKZIP, read the checksum bytes
            if (!IsPKZIP)
            {
                // Seek to the true end of the data
                data.Seek(current + bytesRead, SeekOrigin.Begin);

                // If the read value is off-by-one after checksum
                if (bytesRead == expectedBytesRead - 5)
                {
                    // If not at the end of the file, get the corrected offset
                    if (data.Position + 5 < data.Length)
                    {
                        // TODO: What does this byte represent?
                        byte padding = data.ReadByteValue();
                        bytesRead += 1;

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
                if (data.Position + 4 < data.Length)
                {
                    deflateCrc = data.ReadUInt32LittleEndian();
                    bytesRead += 4;
                }
                // Otherwise, read what is possible and pad with 0x00
                else
                {
                    byte[] deflateCrcBytes = new byte[4];
                    int realCrcLength = data.Read(deflateCrcBytes, 0, (int)(data.Length - data.Position));

                    // Parse as a little-endian 32-bit value
                    deflateCrc = (uint)(deflateCrcBytes[0]
                               | (deflateCrcBytes[1] << 8)
                               | (deflateCrcBytes[2] << 16)
                               | (deflateCrcBytes[3] << 24));

                    bytesRead += realCrcLength;
                }

                // If the CRC to check isn't set
                if (expectedCrc == 0)
                    expectedCrc = deflateCrc;

                // Debug output
                if (includeDebug) Console.WriteLine($"DeflateStream CRC-32: {deflateCrc:X8}");
            }

            // Otherwise, account for the header bytes read
            else
            {
                bytesRead += zipHeaderBytes;
                data.Seek(current + bytesRead, SeekOrigin.Begin);
            }

            // Debug output
            if (includeDebug) Console.WriteLine($"Actual Read: {bytesRead}, Actual Write: {bytesWritten}, Actual CRC-32: {writtenCrc:X8}");

            // If there's a mismatch during both reading and writing
            if (expectedBytesRead >= 0 && expectedBytesRead != bytesRead)
            {
                // This in/out check helps catch false positives, such as
                // files that have an off-by-one mismatch for read values
                // but properly match the output written values.

                // If the written bytes not correct as well
                if (expectedBytesWritten >= 0 && expectedBytesWritten != bytesWritten)
                {
                    // Null the output stream
                    extracted = null;

                    if (includeDebug) Console.Error.WriteLine($"Mismatched read/write values for {filename}!");
                    data.Seek(current, SeekOrigin.Begin);
                    return ExtractStatus.WRONG_SIZE;
                }

                // If the written bytes are not being verified
                else if (expectedBytesWritten < 0)
                {
                    // Null the output stream
                    extracted = null;

                    if (includeDebug) Console.Error.WriteLine($"Mismatched read/write values for {filename}!");
                    data.Seek(current, SeekOrigin.Begin);
                    return ExtractStatus.WRONG_SIZE;
                }
            }

            // If there's just a mismatch during only writing
            if (expectedBytesRead >= 0 && expectedBytesRead == bytesRead)
            {
                // We want to log this but ignore the error
                if (expectedBytesWritten >= 0 && expectedBytesWritten != bytesWritten)
                {
                    if (includeDebug) Console.WriteLine($"Ignoring mismatched write values for {filename} because read values match!");
                }
            }

            // Otherwise, the write size should be checked normally
            else if (expectedBytesRead == 0 && expectedBytesWritten >= 0 && expectedBytesWritten != bytesWritten)
            {
                // Null the output stream
                extracted = null;

                if (includeDebug) Console.Error.WriteLine($"Mismatched write values for {filename}!");
                data.Seek(current, SeekOrigin.Begin);
                return ExtractStatus.WRONG_SIZE;
            }

            // If there's a mismatch with the CRC-32
            if (expectedCrc != 0 && expectedCrc != writtenCrc)
            {
                // Null the output stream
                extracted = null;

                if (includeDebug) Console.Error.WriteLine($"Mismatched CRC-32 values for {filename}!");
                data.Seek(current, SeekOrigin.Begin);
                return ExtractStatus.BAD_CRC;
            }

            return ExtractStatus.GOOD;
        }

        /// <summary>
        /// Attempt to extract a file defined by a file header
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="dataStart">Start of the deflated data</param>
        /// <param name="obj">Deflate information</param>
        /// <param name="index">File index for automatic naming</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted successfully, false otherwise</returns>
        public ExtractStatus ExtractFile(Stream data,
            long dataStart,
            InstallFile obj,
            int index,
            string outputDirectory,
            bool includeDebug)
        {
            // Get expected values
            long expectedBytesRead = obj.DeflateEnd - obj.DeflateStart;
            long expectedBytesWritten = obj.InflatedSize;
            uint expectedCrc = obj.Crc32;

            // Perform path replacements
            string filename = obj.DestinationPathname ?? $"WISE{index:X4}";
            filename = filename.Replace("%", string.Empty);
            data.Seek(dataStart + obj.DeflateStart, SeekOrigin.Begin);
            return ExtractFile(data, filename, outputDirectory, expectedBytesRead, expectedBytesWritten, expectedCrc, includeDebug);
        }

        /// <summary>
        /// Attempt to extract a file defined by a file header
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="dataStart">Start of the deflated data</param>
        /// <param name="obj">Deflate information</param>
        /// <param name="index">File index for automatic naming</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted successfully, false otherwise</returns>
        public ExtractStatus ExtractFile(Stream data,
            long dataStart,
            Unknown0x06 obj,
            int index,
            string outputDirectory,
            bool includeDebug)
        {
            // Get the generated base name
            string baseName = $"WISE_0x06_{obj.Operand_1:X4}";

            // If there are no deflate objects
            if (obj.DeflateInfo?.Info == null)
            {
                if (includeDebug) Console.WriteLine($"Skipping {baseName} because the deflate object array is null!");
                return ExtractStatus.FAIL;
            }

            // Loop through the values
            for (int i = 0; i < obj.DeflateInfo.Info.Length; i++)
            {
                // Get the deflate info object
                var info = obj.DeflateInfo.Info[i];

                // Get expected values
                long expectedBytesRead = info.DeflateEnd - info.DeflateStart;
                long expectedBytesWritten = info.InflatedSize;

                // Perform path replacements
                string filename = $"{baseName}{index:X4}";
                data.Seek(dataStart + info.DeflateStart, SeekOrigin.Begin);
                _ = ExtractFile(data, filename, outputDirectory, expectedBytesRead, expectedBytesWritten, expectedCrc: 0, includeDebug);
            }

            // Always return good -- TODO: Fix this
            return ExtractStatus.GOOD;
        }

        /// <summary>
        /// Attempt to extract a file defined by a file header
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="dataStart">Start of the deflated data</param>
        /// <param name="obj">Deflate information</param>
        /// <param name="index">File index for automatic naming</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted successfully, false otherwise</returns>
        public ExtractStatus ExtractFile(Stream data,
            long dataStart,
            CustomDialogSet obj,
            int index,
            string outputDirectory,
            bool includeDebug)
        {
            // Get expected values
            long expectedBytesRead = obj.DeflateEnd - obj.DeflateStart;
            long expectedBytesWritten = obj.InflatedSize;

            // Perform path replacements
            string filename = $"WISE_0x14_{obj.DisplayVariable}-{obj.Name}";
            filename = filename.Replace("%", string.Empty);
            data.Seek(dataStart + obj.DeflateStart, SeekOrigin.Begin);
            return ExtractFile(data, filename, outputDirectory, expectedBytesRead, expectedBytesWritten, expectedCrc: 0, includeDebug);
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
            if (ExtractFile(data, "WiseColors.dib", outputDirectory, DibDeflatedSize, DibInflatedSize, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract WiseScript.bin
            if (ExtractFile(data, "WiseScript.bin", outputDirectory, WiseScriptDeflatedSize, WiseScriptInflatedSize, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract WISE0001.DLL, if it exists
            if (ExtractFile(data, "WISE0001.DLL", outputDirectory, WiseDllDeflatedSize, -1, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract CTL3D32.DLL, if it exists
            // Has size but shouldn't be read for:
            // - 4_2_19228_Vfw95-98.EXE
            // - DTV39data.EXE
            // - hcwsubid_setup.EXE
            // - InstallAlabamaSmithEscapePompeii.exe
            // - Wintv2K.EXE
            if (ExtractFile(data, "CTL3D32.DLL", outputDirectory, Ctl3d32DeflatedSize, -1, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract FILE0004, if it exists
            if (ExtractFile(data, "FILE0004", outputDirectory, SomeData4DeflatedSize, -1, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract Ocxreg32.EXE, if it exists
            // Has size but shouldn't be read for:
            // - 4_2_19228_Vfw95-98.EXE
            // - DSETUP.EXE
            // - DTV39data.EXE
            // - Wintv2K.EXE
            if (ExtractFile(data, "Ocxreg32.EXE", outputDirectory, RegToolDeflatedSize, -1, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract PROGRESS.DLL, if it exists
            if (ExtractFile(data, "PROGRESS.DLL", outputDirectory, ProgressDllDeflatedSize, -1, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract FILE0007, if it exists
            if (ExtractFile(data, "FILE0007", outputDirectory, SomeData7DeflatedSize, -1, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract FILE0008, if it exists
            if (ExtractFile(data, "FILE0008", outputDirectory, SomeData8DeflatedSize, -1, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract FILE0009, if it exists
            if (ExtractFile(data, "FILE0009", outputDirectory, SomeData9DeflatedSize, -1, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract FILE000A, if it exists
            if (ExtractFile(data, "FILE000A", outputDirectory, SomeData10DeflatedSize, -1, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract install script, if it exists
            if (ExtractFile(data, "INSTALL_SCRIPT", outputDirectory, InstallScriptDeflatedSize, -1, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            // Extract FILE000{n}.DAT, if it exists
            if (ExtractFile(data, IsPKZIP ? null : "FILE00XX.DAT", outputDirectory, FinalFileDeflatedSize, FinalFileInflatedSize, 0, includeDebug) == ExtractStatus.FAIL)
                return false;

            dataStart = data.Position;
            return true;
        }

        /// <summary>
        /// Inflate an input stream to an output file path
        /// </summary>
        private static bool Inflate(Stream input,
            string outputPath,
            out long inputSize,
            out long outputSize,
            out uint crc)
        {
            var output = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            return Inflate(input, output, out inputSize, out outputSize, out crc);
        }

        /// <summary>
        /// Inflate an input stream to an output stream
        /// </summary>
        private static bool Inflate(Stream input,
            Stream output,
            out long inputSize,
            out long outputSize,
            out uint crc)
        {
            inputSize = 0;
            outputSize = 0;
            crc = 0;

            var hasher = new HashWrapper(HashType.CRC32);
            try
            {
                long start = input.Position;
                var ds = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true);
                while (true)
                {
                    byte[] buf = new byte[1024];
                    int read = ds.Read(buf, 0, buf.Length);
                    if (read == 0)
                        break;

                    hasher.Process(buf, 0, read);
                    output.Write(buf, 0, read);
                }

                // Save the deflate values
                inputSize = ds.TotalIn;
                outputSize = ds.TotalOut;
            }
            catch
            {
                return false;
            }

            hasher.Terminate();
            byte[] hashBytes = hasher.CurrentHashBytes!;
            crc = BitConverter.ToUInt32(hashBytes, 0);
            return true;
        }

        /// <summary>
        /// Process the state machine and perform all required actions
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="sourceDirectory">Directory where installer files live, if possible</param>
        /// <param name="script">Parsed script to retrieve information from</param>
        /// <param name="dataStart">Start of the deflated data</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if there were no errors during processing, false otherwise</returns>
        private bool ProcessStateMachine(Stream data,
            string? sourceDirectory,
            WiseScript script,
            long dataStart,
            string outputDirectory,
            bool includeDebug)
        {
            // If the state machine is invalid
            if (script?.States == null || script.States.Length == 0)
                return false;

            // Initialize important loop information
            int normalFileCount = 0;
            Dictionary<string, string> environment = [];
            if (sourceDirectory != null)
                environment.Add("INST", sourceDirectory);

            // Loop through the state machine and process
            foreach (var state in script.States)
            {
                switch (state.Op)
                {
                    case OperationCode.InstallFile:
                        if (state.Data is not InstallFile fileHeader)
                            return false;

                        // Try to extract to the output directory
                        ExtractFile(data, dataStart, fileHeader, ++normalFileCount, outputDirectory, includeDebug);
                        break;

                    case OperationCode.EditIniFile:
                        if (state.Data is not EditIniFile editIniFile)
                            return false;

                        // Ensure directory separators are consistent
                        string iniFilePath = editIniFile.Pathname ?? $"WISE{normalFileCount:X4}.ini";
                        if (Path.DirectorySeparatorChar == '\\')
                            iniFilePath = iniFilePath.Replace('/', '\\');
                        else if (Path.DirectorySeparatorChar == '/')
                            iniFilePath = iniFilePath.Replace('\\', '/');

                        // Perform path replacements
                        foreach (var kvp in environment)
                        {
                            iniFilePath = iniFilePath.Replace($"%{kvp.Key}%", kvp.Value);
                        }

                        iniFilePath = iniFilePath.Replace("%", string.Empty);

                        // Ensure the full output directory exists
                        iniFilePath = Path.Combine(outputDirectory, iniFilePath);
                        var directoryName = Path.GetDirectoryName(iniFilePath);
                        if (directoryName != null && !Directory.Exists(directoryName))
                            Directory.CreateDirectory(directoryName);

                        using (var iniFile = File.Open(iniFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            iniFile.Write(Encoding.ASCII.GetBytes($"[{editIniFile.Section}]\n"));
                            iniFile.Write(Encoding.ASCII.GetBytes($"{editIniFile.Values ?? string.Empty}\n"));
                            iniFile.Flush();
                        }

                        break;

                    case OperationCode.UnknownDeflatedFile0x06:
                        if (state.Data is not Unknown0x06 unknown0x06)
                            return false;

                        // Try to extract to the output directory
                        ExtractFile(data, dataStart, unknown0x06, ++normalFileCount, outputDirectory, includeDebug);
                        break;

                    case OperationCode.DeleteFile:
                        if (state.Data is not DeleteFile deleteFile)
                            return false;

                        if (includeDebug) Console.WriteLine($"File {deleteFile.Pathname} is supposed to be deleted");
                        break;

                    case OperationCode.CreateDirectory:
                        if (state.Data is not CreateDirectory createDirectory)
                            return false;
                        if (createDirectory.Pathname == null)
                            return false;

                        try
                        {
                            if (includeDebug) Console.WriteLine($"Directory {createDirectory.Pathname} is being created");

                            // Ensure directory separators are consistent
                            string newDirectoryName = Path.Combine(outputDirectory, createDirectory.Pathname);
                            if (Path.DirectorySeparatorChar == '\\')
                                newDirectoryName = newDirectoryName.Replace('/', '\\');
                            else if (Path.DirectorySeparatorChar == '/')
                                newDirectoryName = newDirectoryName.Replace('\\', '/');

                            // Perform path replacements
                            foreach (var kvp in environment)
                            {
                                newDirectoryName = newDirectoryName.Replace($"%{kvp.Key}%", kvp.Value);
                            }

                            newDirectoryName = newDirectoryName.Replace("%", string.Empty);

                            // Remove wildcards from end of the path
                            if (newDirectoryName.EndsWith("*.*"))
                                newDirectoryName = newDirectoryName.Substring(0, newDirectoryName.Length - 4);

                            Directory.CreateDirectory(newDirectoryName);
                        }
                        catch
                        {
                            if (includeDebug) Console.WriteLine($"Directory {createDirectory.Pathname} could not be created!");
                        }
                        break;

                    case OperationCode.CopyLocalFile:
                        if (state.Data is not CopyLocalFile copyLocalFile)
                            return false;
                        if (copyLocalFile.Source == null)
                            return false;
                        if (copyLocalFile.Destination == null)
                            return false;

                        try
                        {
                            if (includeDebug) Console.WriteLine($"File {copyLocalFile.Source} is being copied to {copyLocalFile.Destination}");

                            // Ensure directory separators are consistent
                            string oldFilePath = copyLocalFile.Source;
                            if (Path.DirectorySeparatorChar == '\\')
                                oldFilePath = oldFilePath.Replace('/', '\\');
                            else if (Path.DirectorySeparatorChar == '/')
                                oldFilePath = oldFilePath.Replace('\\', '/');

                            // Perform path replacements
                            foreach (var kvp in environment)
                            {
                                oldFilePath = oldFilePath.Replace($"%{kvp.Key}%", kvp.Value);
                            }

                            oldFilePath = oldFilePath.Replace("%", string.Empty);

                            // Sanity check
                            if (!File.Exists(oldFilePath))
                            {
                                if (includeDebug) Console.WriteLine($"File {copyLocalFile.Source} is supposed to be copied to {copyLocalFile.Destination}, but it does not exist!");
                                break;
                            }

                            // Ensure directory separators are consistent
                            string newFilePath = Path.Combine(outputDirectory, copyLocalFile.Destination);
                            if (Path.DirectorySeparatorChar == '\\')
                                newFilePath = newFilePath.Replace('/', '\\');
                            else if (Path.DirectorySeparatorChar == '/')
                                newFilePath = newFilePath.Replace('\\', '/');

                            // Perform path replacements
                            foreach (var kvp in environment)
                            {
                                newFilePath = newFilePath.Replace($"%{kvp.Key}%", kvp.Value);
                            }

                            newFilePath = newFilePath.Replace("%", string.Empty);

                            File.Copy(oldFilePath, newFilePath);
                        }
                        catch
                        {
                            if (includeDebug) Console.WriteLine($"File {copyLocalFile.Source} could not be copied!");
                        }

                        break;

                    case OperationCode.CustomDialogSet:
                        if (state.Data is not CustomDialogSet customDialogSet)
                            return false;

                        // Try to extract to the output directory
                        ExtractFile(data, dataStart, customDialogSet, ++normalFileCount, outputDirectory, includeDebug);
                        break;

                    case OperationCode.GetTemporaryFilename:
                        if (state.Data is not GetTemporaryFilename getTemporaryFilename)
                            return false;

                        if (getTemporaryFilename.Variable != null)
                            environment[getTemporaryFilename.Variable] = Guid.NewGuid().ToString();
                        break;

                    case OperationCode.AddTextToInstallLog:
                        if (state.Data is not AddTextToInstallLog addTextToInstallLog)
                            return false;

                        if (includeDebug) Console.WriteLine($"INSTALL.LOG: {addTextToInstallLog.Text}");
                        break;

                    default:
                        break;
                }
            }

            return true;
        }

        #endregion

        #region Serialization -- Move to Serialization once Models is updated

        /// <summary>
        /// Address of the overlay, if it exists
        /// </summary>
        /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/exefile.c"/>
        private static int GetOverlayAddress(NewExecutable nex)
        {
            // Get the end of the file, if possible
            int endOfFile = nex.GetEndOfFile();
            if (endOfFile == -1)
                return -1;

            // If a required property is missing
            if (nex.Model.Header == null || nex.Model.SegmentTable == null || nex.Model.ResourceTable?.ResourceTypes == null)
                return -1;

            // Search through the segments table to find the furthest
            int endOfSectionData = -1;
            foreach (var entry in nex.Model.SegmentTable)
            {
                int offset = (entry.Offset << nex.Model.Header.SegmentAlignmentShiftCount) + entry.Length;
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

            // Adjust the position of the data by 705 bytes
            // TODO: Investigate what the byte data is
            endOfSectionData += 705;

            // Cache and return the position
            return endOfSectionData;
        }

        #endregion
    }
}
