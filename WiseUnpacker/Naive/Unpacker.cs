using System;
using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Models.WiseInstaller;
using SabreTools.Serialization.Wrappers;
using static WiseUnpacker.Common;
using WiseOverlayHeader = SabreTools.Serialization.Deserializers.WiseOverlayHeader;
using WiseScript = SabreTools.Serialization.Deserializers.WiseScript;

namespace WiseUnpacker.Naive
{
    internal class Unpacker : IWiseUnpacker
    {
        #region Instance Variables

        /// <summary>
        /// Input file to read and extract
        /// </summary>
        private readonly ReadOnlyCompositeStream _inputFile;

        #endregion

        /// <summary>
        /// Create a new modified unpacker
        /// </summary>
        public Unpacker(string file)
        {
            if (!OpenFile(file, out var stream) || stream == null)
                throw new FileNotFoundException(nameof(file));

            _inputFile = stream;
        }

        /// <summary>
        /// Create a new modified unpacker
        /// </summary>
        public Unpacker(Stream stream)
        {
            // Default options
            _inputFile = new ReadOnlyCompositeStream(stream);
        }

        /// <inheritdoc/>
        public bool Run(string outputPath)
        {
            // Attempt to deserialize the file as either NE or PE
            var wrapper = WrapperFactory.CreateExecutableWrapper(_inputFile);
            if (wrapper is not NewExecutable && wrapper is not PortableExecutable)
                return false;

            // Get the overlay offset
            int overlayOffset = wrapper switch
            {
                NewExecutable nex => GetOverlayAddress(nex),
                PortableExecutable pex => pex.OverlayAddress,
                _ => -1,
            };

            // Validate the overlay offset
            if (overlayOffset < 0 || overlayOffset >= _inputFile.Length)
            {
                Close();
                return false;
            }

            // Seek to the overlay
            _inputFile.Seek(overlayOffset, SeekOrigin.Begin);

            // Attempt to parse the overlay data as a header
            var overlayDeserializer = new WiseOverlayHeader();
            var header = overlayDeserializer.Deserialize(_inputFile);
            if (header == null)
            {
                Close();
                return false;
            }

            // Get if the format is PKZIP packed or not
#if NET20 || NET35
            bool pkzip = (header.Flags & OverlayHeaderFlags.WISE_FLAG_PK_ZIP) != 0;
#else
            bool pkzip = header.Flags.HasFlag(OverlayHeaderFlags.WISE_FLAG_PK_ZIP);
#endif

            // Extract WiseColors.dib
            long offset = _inputFile.Position;
            if (header.DibDeflatedSize > 0 && !ExtractFile("WiseColors.dib", outputPath, pkzip))
            {
                Close();
                return false;
            }

            // Extract WiseScript.bin
            _inputFile.Seek(offset + header.DibDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (header.WiseScriptDeflatedSize > 0 && !ExtractFile("WiseScript.bin", outputPath, pkzip))
            {
                Close();
                return false;
            }

            // Extract WISE0001.DLL, if it exists
            _inputFile.Seek(offset + header.WiseScriptDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (header.WiseDllDeflatedSize > 0 && !ExtractFile("WISE0001.DLL", outputPath, pkzip))
            {
                Close();
                return false;
            }

            // Extract PROGRESS.DLL, if it exists
            _inputFile.Seek(offset + header.WiseDllDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (header.ProgressDllDeflatedSize > 0 && !ExtractFile("PROGRESS.DLL", outputPath, pkzip))
            {
                Close();
                return false;
            }

            // Extract FILE000X.DLL, if it exists
            _inputFile.Seek(offset + header.ProgressDllDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (header.SomeData5DeflatedSize > 0 && !ExtractFile(null, outputPath, pkzip))
            {
                Close();
                return false;
            }

            // Open WiseScript.bin for parsing
            var scriptStream = File.OpenRead(Path.Combine(outputPath, "WiseScript.bin"));
            var scriptDeserializer = new WiseScript();
            var scriptFile = scriptDeserializer.Deserialize(scriptStream);
            if (scriptFile == null)
            {
                Close();
                return false;
            }

            // Process the state machine
            long dataStart = offset + header.SomeData5DeflatedSize;
            bool success = ProcessStateMachine(outputPath, pkzip, dataStart, scriptFile.States);

            // Close and return
            Close();
            return true;
        }

        /// <summary>
        /// Process the state machine and perform all required actions
        /// </summary>
        private bool ProcessStateMachine(string outputPath, bool pkzip, long dataStart, MachineState[]? stateMachine)
        {
            // If the state machine is invalid
            if (stateMachine == null || stateMachine.Length == 0)
                return false;

            // Initialize important loop information
            int normalFileCount = 0;
            int unknown0x06FileCount = 0;
            int unknown0x14FileCount = 0;
            string? tempPath = null;

            // Loop through the state machine and process
            foreach (var state in stateMachine)
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
                        _inputFile.Seek(dataStart + fileHeader.DeflateStart, SeekOrigin.Begin);
                        if (!ExtractFile(destFile, outputPath, pkzip))
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
                            _inputFile.Seek(dataStart + info.DeflateStart, SeekOrigin.Begin);
                            if (!ExtractFile(u06Name, outputPath, pkzip))
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
                        _inputFile.Seek(dataStart + u14.DeflateStart, SeekOrigin.Begin);
                        if (!ExtractFile(u14Name, outputPath, pkzip))
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
        /// Attempt to extract WiseColors.dib
        /// </summary>
        /// TODO: Add CRC and size verification
        private bool ExtractFile(string? filename, string outputPath, bool pkzip)
        {
            // Get an inflater to use
            var inflater = new Inflater();

            // Skip the PKZIP header, if it exists
            string? zipName = null;
            if (pkzip)
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
        /// Close the possible Wise installer
        /// </summary>
        private void Close()
        {
            _inputFile?.Close();
        }

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