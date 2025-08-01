using System;
using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Models.WiseInstaller;
using SabreTools.Serialization.Wrappers;
using WiseOverlayHeader = SabreTools.Serialization.Deserializers.WiseOverlayHeader;
using WiseScript = SabreTools.Serialization.Deserializers.WiseScript;

namespace WiseUnpacker
{
    public class Unpacker
    {
        #region Instance Variables

        /// <summary>
        /// Indicates the start of the deflated data
        /// </summary>
        private long _dataStart;

        /// <summary>
        /// Input file to read and extract
        /// </summary>
        private readonly ReadOnlyCompositeStream _inputFile;

        /// <summary>
        /// Indicates if the file uses PKZIP
        /// </summary>
        private bool _isPkZip;

        /// <summary>
        /// Overlay header information
        /// </summary>
        private OverlayHeader? _overlayHeader;

        /// <summary>
        /// Script file information
        /// </summary>
        private ScriptFile? _scriptFile;

        #endregion

        /// <summary>
        /// Create a new modified unpacker
        /// </summary>
        public Unpacker(string file)
        {
            if (!OpenFile(file, out var stream) || stream == null)
                throw new FileNotFoundException(nameof(file));

            _inputFile = stream;
            SetOverlayHeader();
        }

        /// <summary>
        /// Create a new modified unpacker
        /// </summary>
        public Unpacker(Stream stream)
        {
            _inputFile = new ReadOnlyCompositeStream(stream);
            SetOverlayHeader();
        }

        /// <summary>
        /// Attempt to parse, extract, and rename all files from a WISE installer
        /// </summary>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public bool Run(string? outputPath)
        {
            // Ensure the output path
            outputPath ??= string.Empty;

            // Extract the header-defined files
            bool extracted = ExtractHeaderDefinedFiles(outputPath);
            if (!extracted)
            {
                Close();
                return false;
            }

            // Open WiseScript.bin for parsing
            SetScriptFile(outputPath);
            if (_scriptFile == null)
            {
                Close();
                return false;
            }

            // Process the state machine
            bool success = ProcessStateMachine(outputPath);

            // Close and return
            Close();
            return success;
        }

        /// <summary>
        /// Close the possible Wise installer
        /// </summary>
        private void Close()
        {
            _inputFile?.Close();
        }

        #region Helpers

        /// <summary>
        /// Extract the predefined, static files defined in the header
        /// </summary>
        private bool ExtractHeaderDefinedFiles(string outputPath)
        {
            // Validate the overlay header
            if (_overlayHeader == null)
                return false;

            // Extract WiseColors.dib
            long offset = _inputFile.Position;
            if (_overlayHeader.DibDeflatedSize > 0 && !ExtractFile("WiseColors.dib", outputPath))
            {
                return false;
            }

            // Extract WiseScript.bin
            _inputFile.Seek(offset + _overlayHeader.DibDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (_overlayHeader.WiseScriptDeflatedSize > 0 && !ExtractFile("WiseScript.bin", outputPath))
            {
                return false;
            }

            // Extract WISE0001.DLL, if it exists
            _inputFile.Seek(offset + _overlayHeader.WiseScriptDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (_overlayHeader.WiseDllDeflatedSize > 0 && !ExtractFile("WISE0001.DLL", outputPath))
            {
                return false;
            }

            // Extract PROGRESS.DLL, if it exists
            _inputFile.Seek(offset + _overlayHeader.WiseDllDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (_overlayHeader.ProgressDllDeflatedSize > 0 && !ExtractFile("PROGRESS.DLL", outputPath))
            {
                return false;
            }

            // Extract FILE000X.DLL, if it exists
            _inputFile.Seek(offset + _overlayHeader.ProgressDllDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (_overlayHeader.SomeData5DeflatedSize > 0 && !ExtractFile(null, outputPath))
            {
                return false;
            }

            // Set the data start
            _dataStart = offset + _overlayHeader.SomeData5DeflatedSize;

            return true;
        }

        /// <summary>
        /// Attempt to extract WiseColors.dib
        /// </summary>
        /// TODO: Add CRC and size verification
        private bool ExtractFile(string? filename, string outputPath)
        {
            // Get an inflater to use
            var inflater = new Inflater();

            // Skip the PKZIP header, if it exists
            string? zipName = null;
            if (_isPkZip)
                ReadPKZIPHeader(_inputFile, out _, out _, out zipName);

            // Set the name from the zip header if missing
            filename ??= zipName ?? Guid.NewGuid().ToString();

            // Ensure directory separators are consistent
            if (Path.DirectorySeparatorChar == '\\')
                filename = filename.Replace('/', '\\');
            else if (Path.DirectorySeparatorChar == '/')
                filename = filename.Replace('\\', '/');

            // Ensure the full output directory exists
            filename = Path.Combine(outputPath, filename);
            var directoryName = Path.GetDirectoryName(filename);
            if (directoryName != null && !Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            // Extract the file
            if (!inflater.Inflate(_inputFile, filename))
            {
                Console.Error.WriteLine($"Could not extract {filename}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the overlay offset for the input file
        /// </summary>
        private int GetOverlayOffset()
        {
            // Attempt to deserialize the file as either NE or PE
            var wrapper = WrapperFactory.CreateExecutableWrapper(_inputFile);
            if (wrapper is not NewExecutable && wrapper is not PortableExecutable)
            {
                return -1;
            }

            // Get the overlay offset
            return wrapper switch
            {
                NewExecutable nex => GetOverlayAddress(nex),
                PortableExecutable pex => pex.OverlayAddress,
                _ => -1,
            };
        }

        /// <summary>
        /// Open a potential WISE installer file and any additional files
        /// </summary>
        /// <returns>True if the file could be opened, false otherwise</returns>
        private static bool OpenFile(string name, out ReadOnlyCompositeStream? stream)
        {
            // If the file exists as-is
            if (File.Exists(name))
            {
                var fileStream = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream = new ReadOnlyCompositeStream([fileStream]);

                // Strip the extension
                name = Path.GetFileNameWithoutExtension(name);
            }

            // If the base name was provided, try to open the associated exe
            else if (File.Exists($"{name}.exe"))
            {
                var fileStream = File.Open($"{name}.exe", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream = new ReadOnlyCompositeStream([fileStream]);
            }

            // Otherwise, the file cannot be opened
            else
            {
                stream = null;
                return false;
            }

            // Loop through and try to read all additional files
            byte fileno = 2;
            string extraPath = $"{name}.W{fileno:X}";
            while (File.Exists(extraPath))
            {
                var fileStream = File.Open(extraPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream.AddStream(fileStream);
                fileno++;
                extraPath = $"{name}.W{fileno:X}";
            }

            return true;
        }

        /// <summary>
        /// Process the state machine and perform all required actions
        /// </summary>
        private bool ProcessStateMachine(string outputPath)
        {
            // If the state machine is invalid
            if (_scriptFile?.States  == null || _scriptFile.States.Length == 0)
                return false;

            // Initialize important loop information
            int normalFileCount = 0;
            int unknown0x06FileCount = 0;
            int unknown0x14FileCount = 0;
            string? tempPath = null;

            // Loop through the state machine and process
            foreach (var state in _scriptFile.States)
            {
                switch (state.Op)
                {
                    case OperationCode.CustomDeflateFileHeader:
                        normalFileCount++;
                        if (state.Data is not ScriptFileHeader fileHeader)
                            return false;

                        // Perform path replacements
                        string destFile = fileHeader.DestFile ?? $"WISE{normalFileCount:X4}";
                        if (tempPath != null)
                            destFile = destFile.Replace($"%{tempPath}%", "tempfile");

                        destFile = destFile.Replace("%", string.Empty);
                        _inputFile.Seek(_dataStart + fileHeader.DeflateStart, SeekOrigin.Begin);
                        if (!ExtractFile(destFile, outputPath))
                            break;

                        break;

                    case OperationCode.IniFile:
                        if (state.Data is not ScriptUnknown0x05 unknown0x05Data)
                            return false;

                        // Ensure directory separators are consistent
                        string iniFilePath = unknown0x05Data.File ?? $"WISE{normalFileCount:X4}.ini";
                        if (Path.DirectorySeparatorChar == '\\')
                            iniFilePath = iniFilePath.Replace('/', '\\');
                        else if (Path.DirectorySeparatorChar == '/')
                            iniFilePath = iniFilePath.Replace('\\', '/');

                        // Perform path replacements
                        if (tempPath != null)
                            iniFilePath = iniFilePath.Replace($"%{tempPath}%", "tempfile");

                        iniFilePath = iniFilePath.Replace("%", string.Empty);

                        // Ensure the full output directory exists
                        iniFilePath = Path.Combine(outputPath, iniFilePath);
                        var directoryName = Path.GetDirectoryName(iniFilePath);
                        if (directoryName != null && !Directory.Exists(directoryName))
                            Directory.CreateDirectory(directoryName);

                        using (var iniFile = File.Open(iniFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            iniFile.Write(Encoding.ASCII.GetBytes($"[{unknown0x05Data.Section}]\n"));
                            iniFile.Write(Encoding.ASCII.GetBytes($"{unknown0x05Data.Values ?? string.Empty}\n"));
                            iniFile.Flush();
                        }

                        break;

                    case OperationCode.UnknownDeflatedFile0x06:
                        if (state.Data is not ScriptUnknown0x06 u06)
                            return false;
                        if (u06.DeflateInfo?.Info == null)
                            break;

                        foreach (var info in u06.DeflateInfo.Info)
                        {
                            unknown0x06FileCount++;

                            // Perform path replacements
                            string u06Name = $"WISE_0x06_{unknown0x06FileCount:X4}";
                            _inputFile.Seek(_dataStart + info.DeflateStart, SeekOrigin.Begin);
                            if (!ExtractFile(u06Name, outputPath))
                                break;
                        }

                        // TODO: Do something with this? It doesn't extract properly?
                        break;

                    case OperationCode.UnknownDeflatedFile0x14:
                        unknown0x14FileCount++;
                        if (state.Data is not ScriptUnknown0x14 u14)
                            return false;

                        // Perform path replacements
                        string u14Name = u14.Name ?? $"WISE_0x14_{unknown0x14FileCount:X4}";
                        if (tempPath != null)
                            u14Name = u14Name.Replace($"%{tempPath}%", "tempfile");

                        u14Name = u14Name.Replace("%", string.Empty);
                        _inputFile.Seek(_dataStart + u14.DeflateStart, SeekOrigin.Begin);
                        if (!ExtractFile(u14Name, outputPath))
                            break;

                        break;

                    case OperationCode.TempFilename:
                        if (state.Data is not ScriptUnknown0x16 unknown0x16Data)
                            return false;

                        tempPath = unknown0x16Data.Name;
                        break;

                    default:
                        //Console.WriteLine($"Skipped opcode {state.Op}");
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Get CRC and Size from the PKZIP header
        /// </summary>
        private static bool ReadPKZIPHeader(Stream input, out uint crc, out uint size, out string? filename)
        {
            filename = null;

            try
            {
                _ = input.ReadUInt32LittleEndian(); // Signature
                _ = input.ReadUInt16LittleEndian(); // Version
                _ = input.ReadUInt16LittleEndian(); // Flags
                _ = input.ReadUInt16LittleEndian(); // Compression method
                _ = input.ReadUInt16LittleEndian(); // Modification time
                _ = input.ReadUInt16LittleEndian(); // Modification date
                crc = input.ReadUInt32LittleEndian();
                size = input.ReadUInt32LittleEndian(); // Compressed size
                _ = input.ReadUInt32LittleEndian(); // Uncompressed size
                ushort filenameLength = input.ReadUInt16LittleEndian();
                ushort extraLength = input.ReadUInt16LittleEndian();

                if (filenameLength > 0)
                {
                    byte[] filenameBytes = input.ReadBytes(filenameLength);
                    filename = Encoding.ASCII.GetString(filenameBytes);
                }
                if (extraLength > 0)
                    _ = input.ReadBytes(extraLength);

                return true;
            }
            catch
            {
                crc = 0;
                size = 0;
                return false;
            }
        }

        /// <summary>
        /// Set the overlay header from the input file
        /// </summary>
        private void SetOverlayHeader()
        {
            // Validate the overlay offset
            int overlayOffset = GetOverlayOffset();
            if (overlayOffset < 0 || overlayOffset >= _inputFile.Length)
            {
                _overlayHeader = null;
                return;
            }

            // Seek to the overlay
            _inputFile.Seek(overlayOffset, SeekOrigin.Begin);

            // Attempt to parse the overlay data as a header
            var overlayDeserializer = new WiseOverlayHeader();
            _overlayHeader = overlayDeserializer.Deserialize(_inputFile);
            if (_overlayHeader == null)
                return;

            // Set if the format is PKZIP packed or not
#if NET20 || NET35
            _isPkZip = (_overlayHeader.Flags & OverlayHeaderFlags.WISE_FLAG_PK_ZIP) != 0;
#else
            _isPkZip = _overlayHeader.Flags.HasFlag(OverlayHeaderFlags.WISE_FLAG_PK_ZIP);
#endif
        }

        /// <summary>
        /// Set the script file from the input file
        /// </summary>
        private void SetScriptFile(string outputPath)
        {
            var scriptStream = File.OpenRead(Path.Combine(outputPath, "WiseScript.bin"));
            var scriptDeserializer = new WiseScript();
            _scriptFile = scriptDeserializer.Deserialize(scriptStream);
        }

        #endregion

        #region Serialization -- Move to Serialization once Models is updated

        /// <summary>
        /// Address of the overlay, if it exists
        /// </summary>
        /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/exefile.c"/>
        internal static int GetOverlayAddress(NewExecutable nex)
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
                endOfSectionData = -1;

            // Adjust the position of the data by 705 bytes
            // TODO: Investigate what the byte data is
            endOfSectionData += 705;

            // Cache and return the position
            return endOfSectionData;
        }

        #endregion
    }
}