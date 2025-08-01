using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Models.WiseInstaller;
using SabreTools.Serialization.Wrappers;
using static WiseUnpacker.Common;

namespace WiseUnpacker.Naive
{
    internal class Unpacker : IWiseUnpacker
    {
        #region Private Classes

        private class WiseState
        {
            public OperationCode Op { get; set; }
            public object? Data { get; set; }
        }

        #endregion

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
            OverlayHeader header;
            try
            {
                header = DeserializeOverlayHeader(_inputFile);
                if (header.Endianness != Endianness.LittleEndian && header.Endianness != Endianness.BigEndian)
                    return false;
            }
            catch
            {
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
                return false;

            // Extract WiseScript.bin
            _inputFile.Seek(offset + header.DibDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (header.WiseScriptDeflatedSize > 0 && !ExtractFile("WiseScript.bin", outputPath, pkzip))
                return false;

            // Extract WISE0001.DLL, if it exists
            _inputFile.Seek(offset + header.WiseScriptDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (header.WiseDllDeflatedSize > 0 && !ExtractFile("WISE0001.DLL", outputPath, pkzip))
                return false;

            // Extract PROGRESS.DLL, if it exists
            _inputFile.Seek(offset + header.WiseDllDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (header.ProgressDllDeflatedSize > 0 && !ExtractFile("PROGRESS.DLL", outputPath, pkzip))
                return false;

            // Extract FILE000X.DLL, if it exists
            _inputFile.Seek(offset + header.ProgressDllDeflatedSize, SeekOrigin.Begin);
            offset = _inputFile.Position;
            if (header.SomeData5DeflatedSize > 0 && !ExtractFile(null, outputPath, pkzip))
                return false;

            // Open WiseScript.bin for parsing
            var scriptStream = File.OpenRead(Path.Combine(outputPath, "WiseScript.bin"));
            var scriptHeader = DeserializeScriptHeader(scriptStream);
            long dataStart = offset + header.SomeData5DeflatedSize;

            // Process the state machine
            var stateMachine = ReadStateMachine(scriptStream, scriptHeader.LanguageCount);
            bool success = ProcessStateMachine(outputPath, pkzip, dataStart, stateMachine);

            // Close and return
            Close();
            return true;
        }

        /// <summary>
        /// Read the state machine from WiseScript.bin
        /// </summary>
        private static List<WiseState> ReadStateMachine(Stream stream, byte languageCount)
        {
            // Initialize important loop information
            int op0x18skip = -1;

            // Store all states in order
            List<WiseState> states = [];

            while (stream.Position < stream.Length)
            {
                var op = (OperationCode)stream.ReadByteValue();
                object? data = op switch
                {
                    OperationCode.CustomDeflateFileHeader => DeserializeScriptFileHeader(stream, languageCount),
                    OperationCode.Unknown0x03 => DeserializeUnknown0x03(stream, languageCount),
                    OperationCode.FormData => DeserializeUnknown0x04(stream, languageCount),
                    OperationCode.IniFile => DeserializeUnknown0x05(stream),
                    OperationCode.UnknownDeflatedFile0x06 => DeserializeUnknown0x06(stream, languageCount),
                    OperationCode.Unknown0x07 => DeserializeUnknown0x07(stream),
                    OperationCode.EndBranch => DeserializeUnknown0x08(stream),
                    OperationCode.FunctionCall => DeserializeUnknown0x09(stream, languageCount),
                    OperationCode.Unknown0x0A => DeserializeUnknown0x0A(stream),
                    OperationCode.Unknown0x0B => DeserializeUnknown0x0B(stream),
                    OperationCode.IfStatement => DeserializeUnknown0x0C(stream),
                    OperationCode.ElseStatement => null, // No-op
                    OperationCode.StartFormData => null, // No-op
                    OperationCode.EndFormData => null, // No-op
                    OperationCode.Unknown0x11 => DeserializeUnknown0x11(stream),
                    OperationCode.FileOnInstallMedium => DeserializeUnknown0x12(stream, languageCount),
                    OperationCode.UnknownDeflatedFile0x14 => DeserializeUnknown0x14(stream),
                    OperationCode.Unknown0x15 => DeserializeUnknown0x15(stream),
                    OperationCode.TempFilename => DeserializeUnknown0x16(stream),
                    OperationCode.Unknown0x17 => DeserializeUnknown0x17(stream),
                    OperationCode.Skip0x18 => null, // No-op, handled below
                    OperationCode.Unknown0x19 => DeserializeUnknown0x19(stream),
                    OperationCode.Unknown0x1A => DeserializeUnknown0x1A(stream),
                    OperationCode.Skip0x1B => null, // No-op
                    OperationCode.Unknown0x1C => DeserializeUnknown0x1C(stream),
                    OperationCode.Unknown0x1D => DeserializeUnknown0x1D(stream),
                    OperationCode.Unknown0x1E => DeserializeUnknown0x1E(stream),
                    OperationCode.ElseIfStatement => DeserializeUnknown0x23(stream),
                    OperationCode.Skip0x24 => null, // No-op
                    OperationCode.Skip0x25 => null, // No-op
                    OperationCode.ReadByteAndStrings => DeserializeUnknown0x30(stream),

                    _ => throw new IndexOutOfRangeException(nameof(op)),
                };

                // Special handling
                if (op == OperationCode.Skip0x18)
                    op0x18skip = DeserializeUnknown0x18(stream, op0x18skip);

                var state = new WiseState
                {
                    Op = op,
                    Data = data,
                };
                states.Add(state);
            }

            return states;
        }

        /// <summary>
        /// Process the state machine and perform all required actions
        /// </summary>
        private bool ProcessStateMachine(string outputPath, bool pkzip, long dataStart, List<WiseState> stateMachine)
        {
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

        /// <summary>
        /// Deserialize the overlay header
        /// </summary>
        internal static OverlayHeader DeserializeOverlayHeader(Stream data)
        {
            var header = new OverlayHeader();

            header.DllNameLen = data.ReadByteValue();
            if (header.DllNameLen > 0)
            {
                byte[] dllName = data.ReadBytes(header.DllNameLen);
                header.DllName = Encoding.ASCII.GetString(dllName);
                header.DllSize = data.ReadUInt32LittleEndian();
            }

            header.Flags = (OverlayHeaderFlags)data.ReadUInt32LittleEndian();
            header.Unknown_20 = data.ReadBytes(20);
            header.WiseScriptInflatedSize = data.ReadUInt32LittleEndian();
            header.WiseScriptDeflatedSize = data.ReadUInt32LittleEndian();
            header.WiseDllDeflatedSize = data.ReadUInt32LittleEndian();
            header.UnknownU32_1 = data.ReadUInt32LittleEndian();
            header.UnknownU32_2 = data.ReadUInt32LittleEndian();
            header.UnknownU32_3 = data.ReadUInt32LittleEndian();
            header.ProgressDllDeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData6DeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData7DeflatedSize = data.ReadUInt32LittleEndian();
            header.Unknown_8 = data.ReadBytes(8);
            header.SomeData5DeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData5InflatedSize = data.ReadUInt32LittleEndian();
            header.EOF = data.ReadUInt32LittleEndian();
            header.DibDeflatedSize = data.ReadUInt32LittleEndian();

            // Handle older overlay data
            if (header.DibDeflatedSize > data.Length)
            {
                data.Seek(-4, SeekOrigin.Current);
                return header;
            }

            header.DibInflatedSize = data.ReadUInt32LittleEndian();
            header.Endianness = (Endianness)data.ReadUInt16LittleEndian();
            header.InitTextLen = data.ReadByteValue();
            if (header.InitTextLen > 0)
            {
                byte[] initText = data.ReadBytes(header.InitTextLen);
                header.InitText = Encoding.ASCII.GetString(initText);
            }

            return header;
        }

        /// <summary>
        /// Deserialize the script deflate info
        /// </summary>
        internal static ScriptDeflateInfo DeserializeScriptDeflateInfo(Stream data)
        {
            var obj = new ScriptDeflateInfo();

            obj.DeflateStart = data.ReadUInt32LittleEndian();
            obj.DeflateEnd = data.ReadUInt32LittleEndian();
            obj.InflatedSize = data.ReadUInt32LittleEndian();

            return obj;
        }

        /// <summary>
        /// Deserialize the script deflate info container
        /// </summary>
        internal static ScriptDeflateInfoContainer DeserializeScriptDeflateInfoContainer(Stream data, int languageCount)
        {
            var obj = new ScriptDeflateInfoContainer();

            obj.Info = new ScriptDeflateInfo[languageCount];
            for (int i = 0; i < obj.Info.Length; i++)
            {
                obj.Info[i] = DeserializeScriptDeflateInfo(data);
            }

            return obj;
        }

        /// <summary>
        /// Deserialize the script file header
        /// </summary>
        internal static ScriptFileHeader DeserializeScriptFileHeader(Stream data, int languageCount)
        {
            var header = new ScriptFileHeader();

            header.Unknown_2 = data.ReadUInt16LittleEndian();
            header.DeflateStart = data.ReadUInt32LittleEndian();
            header.DeflateEnd = data.ReadUInt32LittleEndian();
            header.Date = data.ReadUInt16LittleEndian();
            header.Time = data.ReadUInt16LittleEndian();
            header.InflatedSize = data.ReadUInt32LittleEndian();
            header.Unknown_20 = data.ReadBytes(20);
            header.Crc32 = data.ReadUInt32LittleEndian();
            header.DestFile = data.ReadNullTerminatedAnsiString();

            header.FileTexts = new string[languageCount];
            for (int i = 0; i < header.FileTexts.Length; i++)
            {
                header.FileTexts[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            header.UnknownString = data.ReadNullTerminatedAnsiString();

            return header;
        }

        /// <summary>
        /// Deserialize the script header
        /// </summary>
        internal static ScriptHeader DeserializeScriptHeader(Stream data)
        {
            var header = new ScriptHeader();

            header.Unknown_5 = data.ReadBytes(5);
            header.SomeOffset1 = data.ReadUInt32LittleEndian();
            header.SomeOffset2 = data.ReadUInt32LittleEndian();
            header.Unknown_4 = data.ReadBytes(4);
            header.DateTime = data.ReadUInt32LittleEndian();
            header.Unknown_22 = data.ReadBytes(22);
            header.Url = data.ReadNullTerminatedAnsiString();
            header.LogPath = data.ReadNullTerminatedAnsiString();
            header.Font = data.ReadNullTerminatedAnsiString();
            header.Unknown_6 = data.ReadBytes(6);
            header.LanguageCount = data.ReadByteValue();

            header.UnknownStrings_7 = new string[7];
            for (int i = 0; i < header.UnknownStrings_7.Length; i++)
            {
                header.UnknownStrings_7[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            int languageSelectionCount = header.LanguageCount == 1 ? 1 : (header.LanguageCount * 2) + 2;
            header.LanguageSelectionStrings = new string[languageSelectionCount];
            for (int i = 0; i < header.LanguageSelectionStrings.Length; i++)
            {
                header.LanguageSelectionStrings[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            header.ScriptStrings = new string[55 * header.LanguageCount];
            for (int i = 0; i < header.ScriptStrings.Length; i++)
            {
                header.ScriptStrings[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            return header;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x03 data
        /// </summary>
        internal static ScriptUnknown0x03 DeserializeUnknown0x03(Stream data, int languageCount)
        {
            var obj = new ScriptUnknown0x03();

            obj.Unknown_1 = data.ReadByteValue();
            obj.LangStrings = new string[languageCount * 2];
            for (int i = 0; i < obj.LangStrings.Length; i++)
            {
                obj.LangStrings[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x04 data
        /// </summary>
        internal static ScriptUnknown0x04 DeserializeUnknown0x04(Stream data, int languageCount)
        {
            var obj = new ScriptUnknown0x04();

            obj.No = data.ReadByteValue();
            obj.LangStrings = new string[languageCount];
            for (int i = 0; i < obj.LangStrings.Length; i++)
            {
                obj.LangStrings[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x05 data
        /// </summary>
        internal static ScriptUnknown0x05 DeserializeUnknown0x05(Stream data)
        {
            var obj = new ScriptUnknown0x05();

            obj.File = data.ReadNullTerminatedAnsiString();
            obj.Section = data.ReadNullTerminatedAnsiString();
            obj.Values = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x06 data
        /// </summary>
        internal static ScriptUnknown0x06 DeserializeUnknown0x06(Stream data, int languageCount)
        {
            var obj = new ScriptUnknown0x06();

            obj.Unknown_2 = data.ReadBytes(2);
            obj.Unknown = data.ReadUInt32LittleEndian();
            obj.DeflateInfo = DeserializeScriptDeflateInfoContainer(data, languageCount);
            obj.Terminator = data.ReadByteValue();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x07 data
        /// </summary>
        internal static ScriptUnknown0x07 DeserializeUnknown0x07(Stream data)
        {
            var obj = new ScriptUnknown0x07();

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_3 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x08 data
        /// </summary>
        internal static ScriptUnknown0x08 DeserializeUnknown0x08(Stream data)
        {
            var obj = new ScriptUnknown0x08();

            obj.Unknown_1 = data.ReadByteValue();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x09 data
        /// </summary>
        internal static ScriptUnknown0x09 DeserializeUnknown0x09(Stream data, int languageCount)
        {
            var obj = new ScriptUnknown0x09();

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_3 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_4 = data.ReadNullTerminatedAnsiString();

            obj.UnknownStrings = new string[languageCount];
            for (int i = 0; i < obj.UnknownStrings.Length; i++)
            {
                obj.UnknownStrings[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x0A data
        /// </summary>
        internal static ScriptUnknown0x0A DeserializeUnknown0x0A(Stream data)
        {
            var obj = new ScriptUnknown0x0A();

            obj.Unknown_2 = data.ReadUInt16LittleEndian();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_3 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x0B data
        /// </summary>
        internal static ScriptUnknown0x0B DeserializeUnknown0x0B(Stream data)
        {
            var obj = new ScriptUnknown0x0B();

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x0C data
        /// </summary>
        internal static ScriptUnknown0x0C DeserializeUnknown0x0C(Stream data)
        {
            var obj = new ScriptUnknown0x0C();

            obj.Unknown_1 = data.ReadByteValue();
            obj.VarName = data.ReadNullTerminatedAnsiString();
            obj.VarValue = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x11 data
        /// </summary>
        internal static ScriptUnknown0x11 DeserializeUnknown0x11(Stream data)
        {
            var obj = new ScriptUnknown0x11();

            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x12 data
        /// </summary>
        internal static ScriptUnknown0x12 DeserializeUnknown0x12(Stream data, int languageCount)
        {
            var obj = new ScriptUnknown0x12();

            obj.Unknown_1 = data.ReadByteValue();
            obj.Unknown_41 = data.ReadBytes(41);
            obj.SourceFile = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();

            obj.UnknownStrings = new string[languageCount];
            for (int i = 0; i < obj.UnknownStrings.Length; i++)
            {
                obj.UnknownStrings[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            obj.DestFile = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x14 data
        /// </summary>
        internal static ScriptUnknown0x14 DeserializeUnknown0x14(Stream data)
        {
            var obj = new ScriptUnknown0x14();

            obj.DeflateStart = data.ReadUInt32LittleEndian();
            obj.DeflateEnd = data.ReadUInt32LittleEndian();
            obj.InflatedSize = data.ReadUInt32LittleEndian();
            obj.Name = data.ReadNullTerminatedAnsiString();
            obj.Message = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x15 data
        /// </summary>
        internal static ScriptUnknown0x15 DeserializeUnknown0x15(Stream data)
        {
            var obj = new ScriptUnknown0x15();

            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x16 data
        /// </summary>
        internal static ScriptUnknown0x16 DeserializeUnknown0x16(Stream data)
        {
            var obj = new ScriptUnknown0x16();

            obj.Name = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x17 data
        /// </summary>
        internal static ScriptUnknown0x17 DeserializeUnknown0x17(Stream data)
        {
            var obj = new ScriptUnknown0x17();

            obj.Unknown_1 = data.ReadByteValue();
            obj.Unknown_4 = data.ReadBytes(4);
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x18 data
        /// </summary>
        internal static int DeserializeUnknown0x18(Stream data, int op0x18skip)
        {
            // If the skip amount needs to be determined
            if (op0x18skip == -1)
            {
                byte nextByte = data.ReadByteValue();
                data.Seek(-1, SeekOrigin.Current);

                op0x18skip = nextByte != 0 ? 0 : 6;
            }

            // Skip additional bytes
            if (op0x18skip > 0)
                _ = data.ReadBytes(op0x18skip);

            return op0x18skip;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x19 data
        /// </summary>
        internal static ScriptUnknown0x19 DeserializeUnknown0x19(Stream data)
        {
            var obj = new ScriptUnknown0x19();

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_3 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x1A data
        /// </summary>
        internal static ScriptUnknown0x1A DeserializeUnknown0x1A(Stream data)
        {
            var obj = new ScriptUnknown0x1A();

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x1C data
        /// </summary>
        internal static ScriptUnknown0x1C DeserializeUnknown0x1C(Stream data)
        {
            var obj = new ScriptUnknown0x1C();

            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        //// <summary>
        /// Deserialize ScriptUnknown0x1D data
        /// </summary>
        internal static ScriptUnknown0x1D DeserializeUnknown0x1D(Stream data)
        {
            var obj = new ScriptUnknown0x1D();

            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x1E data
        /// </summary>
        internal static ScriptUnknown0x1E DeserializeUnknown0x1E(Stream data)
        {
            var obj = new ScriptUnknown0x1E();

            obj.Unknown = data.ReadByteValue();
            obj.UnknownString = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x23 data
        /// </summary>
        internal static ScriptUnknown0x23 DeserializeUnknown0x23(Stream data)
        {
            var obj = new ScriptUnknown0x23();

            obj.Unknown_1 = data.ReadByteValue();
            obj.VarName = data.ReadNullTerminatedAnsiString();
            obj.VarValue = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Deserialize ScriptUnknown0x30 data
        /// </summary>
        internal static ScriptUnknown0x30 DeserializeUnknown0x30(Stream data)
        {
            var obj = new ScriptUnknown0x30();

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        #endregion
    }
}