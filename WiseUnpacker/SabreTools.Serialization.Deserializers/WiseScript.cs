using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.Models.WiseInstaller;
using SabreTools.Models.WiseInstaller.Actions;

namespace SabreTools.Serialization.Deserializers
{
    public class WiseScript : BaseBinaryDeserializer<ScriptFile>
    {
        /// <inheritdoc/>
        public override ScriptFile? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                var script = new ScriptFile();

                #region Header

                var header = ParseScriptHeader(data);
                script.Header = header;

                #endregion

                #region State Machine

                // Flag old/trimmed headers
                bool old = header.Unknown_22?.Length != 22;

                var states = ParseStateMachine(data, header.LanguageCount, old);
                if (states == null)
                    return null;

                script.States = states;

                #endregion

                return script;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into a ScriptDeflateInfo
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptDeflateInfo on success, null on error</returns>
        private static ScriptDeflateInfo ParseScriptDeflateInfo(Stream data)
        {
            var obj = new ScriptDeflateInfo();

            obj.DeflateStart = data.ReadUInt32LittleEndian();
            obj.DeflateEnd = data.ReadUInt32LittleEndian();
            obj.InflatedSize = data.ReadUInt32LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptDeflateInfoContainer
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptDeflateInfoContainer on success, null on error</returns>
        private static ScriptDeflateInfoContainer ParseScriptDeflateInfoContainer(Stream data, int languageCount)
        {
            var obj = new ScriptDeflateInfoContainer();

            obj.Info = new ScriptDeflateInfo[languageCount];
            for (int i = 0; i < obj.Info.Length; i++)
            {
                obj.Info[i] = ParseScriptDeflateInfo(data);
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptHeader
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptHeader on success, null on error</returns>
        private static ScriptHeader ParseScriptHeader(Stream data)
        {
            // Cache the current position in case of a trimmed header
            long current = data.Position;

            var header = new ScriptHeader();

            header.Unknown_5 = data.ReadBytes(5);
            header.SomeOffset1 = data.ReadUInt32LittleEndian();
            header.SomeOffset2 = data.ReadUInt32LittleEndian();
            header.Unknown_4 = data.ReadBytes(4);
            header.DateTime = data.ReadUInt32LittleEndian();
            header.Unknown_22 = data.ReadBytes(22);
            header.Url = data.ReadNullTerminatedAnsiString();
            header.LogPath = data.ReadNullTerminatedAnsiString();
            header.MessageFont = data.ReadNullTerminatedAnsiString();

            // If the font string is empty, then the header is trimmed
            int scriptStringsMultiplier = 55;
            if (header.MessageFont != null && header.MessageFont.Length == 0)
            {
                // Seek back to the original position
                data.Seek(current, SeekOrigin.Begin);

                // Recreate the header with minimal data
                header = new ScriptHeader();

                // TODO: Figure out if this maps to existing fields
                header.Unknown_22 = data.ReadBytes(18);
                header.Url = data.ReadNullTerminatedAnsiString();
                header.LogPath = data.ReadNullTerminatedAnsiString();
                header.MessageFont = data.ReadNullTerminatedAnsiString();
                scriptStringsMultiplier = 46;
            }

            header.FontSize = data.ReadUInt32LittleEndian();
            header.Unknown_2 = data.ReadBytes(2);
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

            header.ScriptStrings = new string[scriptStringsMultiplier * header.LanguageCount];
            for (int i = 0; i < header.ScriptStrings.Length; i++)
            {
                header.ScriptStrings[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            return header;
        }

        /// <summary>
        /// Parse a Stream into a state machine
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="languageCount">Language counter from the header</param>
        /// <param name="old">Indicates an old install script</param>
        /// <returns>Filled state machine on success, null on error</returns>
        private static MachineState[]? ParseStateMachine(Stream data, byte languageCount, bool old)
        {
            // Initialize important loop information
            int op0x18skip = -1;

            // Store all states in order
            List<MachineState> states = [];

            while (data.Position < data.Length)
            {
                var op = (OperationCode)data.ReadByteValue();
                MachineStateData? stateData = op switch
                {
                    OperationCode.InstallFile => ParseScriptFileHeader(data, languageCount),
                    OperationCode.DisplayMessage => ParseDisplayMessage(data, languageCount),
                    OperationCode.FormData => ParseScriptFormData(data, languageCount),
                    OperationCode.EditIniFile => ParseEditIniFile(data),
                    OperationCode.UnknownDeflatedFile0x06 => ParseUnknown0x06(data, languageCount),
                    OperationCode.ExecuteProgram => ParseExecuteProgram(data),
                    OperationCode.EndBlock => ParseEndBlockStatement(data),
                    OperationCode.FunctionCall => ParseExternalDLLCall(data, languageCount, old),
                    OperationCode.EditRegistry => ParseScriptEditRegistry(data),
                    OperationCode.DeleteFile => ParseDeleteFile(data),
                    OperationCode.IfWhileStatement => ParseIfWhileStatement(data),
                    OperationCode.ElseStatement => ParseElseStatement(data),
                    OperationCode.StartFormData => null, // No-op
                    OperationCode.EndFormData => null, // No-op
                    OperationCode.Unknown0x11 => ParseUnknown0x11(data),
                    OperationCode.CopyLocalFile => ParseCopyLocalFile(data, languageCount),
                    OperationCode.CustomDialogSet => ParseCustomDialogSet(data),
                    OperationCode.GetSystemInformation => ParseGetSystemInformation(data),
                    OperationCode.GetTemporaryFilename => ParseGetTemporaryFilename(data),
                    OperationCode.Unknown0x17 => ParseUnknown0x17(data),
                    OperationCode.NewEvent => null, // No-op, handled below
                    OperationCode.Unknown0x19 => ParseUnknown0x19(data),
                    OperationCode.Unknown0x1A => ParseUnknown0x1A(data),
                    OperationCode.IncludeScript => ParseIncludeScript(data),
                    OperationCode.AddTextToInstallLog => ParseAddTextToInstallLog(data),
                    OperationCode.Unknown0x1D => ParseUnknown0x1D(data),
                    OperationCode.CompilerVariableIf => ParseCompilerVariableIf(data),
                    OperationCode.ElseIfStatement => ParseScriptElseIf(data),
                    OperationCode.Skip0x24 => null, // No-op
                    OperationCode.Skip0x25 => null, // No-op
                    OperationCode.ReadByteAndStrings => ParseUnknown0x30(data),

                    _ => throw new IndexOutOfRangeException(nameof(op)),
                };

                // Special handling
                if (op == OperationCode.NewEvent)
                    op0x18skip = ParseNewEvent(data, op0x18skip);

                var state = new MachineState
                {
                    Op = op,
                    Data = stateData,
                };
                states.Add(state);
            }

            return [.. states];
        }

        /// <summary>
        /// Parse a Stream into a ScriptFileHeader
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptFileHeader on success, null on error</returns>
        private static ScriptFileHeader ParseScriptFileHeader(Stream data, int languageCount)
        {
            var header = new ScriptFileHeader();

            header.Operand_1 = data.ReadUInt16LittleEndian();
            header.DeflateStart = data.ReadUInt32LittleEndian();
            header.DeflateEnd = data.ReadUInt32LittleEndian();
            header.Date = data.ReadUInt16LittleEndian();
            header.Time = data.ReadUInt16LittleEndian();
            header.InflatedSize = data.ReadUInt32LittleEndian();
            header.Operand_7 = data.ReadBytes(20);
            header.Crc32 = data.ReadUInt32LittleEndian();
            header.DestFile = data.ReadNullTerminatedAnsiString();

            header.Description = new string[languageCount];
            for (int i = 0; i < header.Description.Length; i++)
            {
                header.Description[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            header.Operand_11 = data.ReadNullTerminatedAnsiString();

            return header;
        }

        /// <summary>
        /// Parse a Stream into a DisplayMessage
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="languageCount">Language counter from the header</param>
        /// <returns>Filled DisplayMessage on success, null on error</returns>
        private static DisplayMessage ParseDisplayMessage(Stream data, int languageCount)
        {
            var obj = new DisplayMessage();

            obj.Flags = data.ReadByteValue();
            obj.TitleText = new string[languageCount * 2];
            for (int i = 0; i < obj.TitleText.Length; i++)
            {
                obj.TitleText[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptFormData
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="languageCount">Language counter from the header</param>
        /// <returns>Filled ScriptFormData on success, null on error</returns>
        private static ScriptFormData ParseScriptFormData(Stream data, int languageCount)
        {
            var obj = new ScriptFormData();

            obj.No = data.ReadByteValue();
            obj.LangStrings = new string[languageCount];
            for (int i = 0; i < obj.LangStrings.Length; i++)
            {
                obj.LangStrings[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an EditIniFile
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled EditIniFile on success, null on error</returns>
        private static EditIniFile ParseEditIniFile(Stream data)
        {
            var obj = new EditIniFile();

            obj.Pathname = data.ReadNullTerminatedAnsiString();
            obj.Section = data.ReadNullTerminatedAnsiString();
            obj.Values = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x06
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="languageCount">Language counter from the header</param>
        /// <returns>Filled ScriptUnknown0x06 on success, null on error</returns>
        private static ScriptUnknown0x06 ParseUnknown0x06(Stream data, int languageCount)
        {
            var obj = new ScriptUnknown0x06();

            obj.Operand_1 = data.ReadBytes(2);
            obj.Operand_2 = data.ReadUInt32LittleEndian();
            obj.DeflateInfo = ParseScriptDeflateInfoContainer(data, languageCount);
            obj.Terminator = data.ReadByteValue();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ExecuteProgram
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ExecuteProgram on success, null on error</returns>
        private static ExecuteProgram ParseExecuteProgram(Stream data)
        {
            var obj = new ExecuteProgram();

            obj.Flags = data.ReadByteValue();
            obj.Pathname = data.ReadNullTerminatedAnsiString();
            obj.CommandLine = data.ReadNullTerminatedAnsiString();
            obj.DefaultDirectory = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a EndBlockStatement
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled EndBlockStatement on success, null on error</returns>
        private static EndBlockStatement ParseEndBlockStatement(Stream data)
        {
            var obj = new EndBlockStatement();

            obj.Operand_1 = data.ReadByteValue();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ExternalDLLCall
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="languageCount">Language counter from the header</param>
        /// <param name="old">Indicates an old install script</param>
        /// <returns>Filled ExternalDLLCall on success, null on error</returns>
        private static ExternalDLLCall ParseExternalDLLCall(Stream data, int languageCount, bool old)
        {
            var obj = new ExternalDLLCall();

            obj.Flags = data.ReadByteValue();
            obj.DllPath = data.ReadNullTerminatedAnsiString();
            obj.FunctionName = data.ReadNullTerminatedAnsiString();
            if (!old)
            {
                obj.Operand_4 = data.ReadNullTerminatedAnsiString();
                obj.ReturnVariable = data.ReadNullTerminatedAnsiString();
            }

            obj.Entries = new string[languageCount];
            for (int i = 0; i < obj.Entries.Length; i++)
            {
                // Read and store the entry string
                obj.Entries[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;

                // Switch based on the function
                // TODO: Remove after mapping is complete
                switch (obj.FunctionName)
                {
                    // Read INI Value
                    case "f8": break;

                    // Get Registry Key Value
                    case "f9": break;

                    // Check Configuration
                    case "f12": break;

                    // Search for File
                    case "f13": break;

                    // Set Variable
                    case "f16": break;

                    // Get Environment Variable
                    case "f17": break;

                    // Check if File/Dir Exists
                    case "f19": break;

                    // Unknown external
                    case "f23":
                        // TODO: Implement
                        // Add ProgMan Icon(?)
                        // Probably this layout:
                        // - Unknown numeric value (e.g. "0")
                        // - Unknown numeric value (e.g. "0")
                        // - Unknown string, empty in samples
                        // - Tab-separated list of components? Locations? Some numeric?
                        break;

                    // Parse String
                    case "f27": break;

                    // Self-Register OCXs/DLLs
                    case "f29": break;

                    // Wizard Block
                    case "f31": break;

                    // Read/Update Text File
                    case "f33": break;

                    // Post to HTTP Server
                    case "f34": break;

                    // External DLL Calls
                    default:
                        string[] parts = obj.Entries[i].Split((char)0x7F);
                        if (string.IsNullOrEmpty(obj.DllPath))
                            Console.WriteLine($"Unrecognized function: {obj.FunctionName} with parts: {string.Join(", ", parts)}");

                        break;
                }
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptEditRegistry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptEditRegistry on success, null on error</returns>
        private static EditRegistry ParseScriptEditRegistry(Stream data)
        {
            var obj = new EditRegistry();

            obj.Root = data.ReadByteValue();
            obj.DataType = data.ReadByteValue(); // TODO: ushort, sometimes?
            obj.Key = data.ReadNullTerminatedAnsiString();
            obj.NewValue = data.ReadNullTerminatedAnsiString();
            obj.ValueName = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a DeleteFile
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled DeleteFile on success, null on error</returns>
        private static DeleteFile ParseDeleteFile(Stream data)
        {
            var obj = new DeleteFile();

            obj.Flags = data.ReadByteValue();
            obj.Pathname = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a IfWhileStatement
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled IfWhileStatement on success, null on error</returns>
        private static IfWhileStatement ParseIfWhileStatement(Stream data)
        {
            var obj = new IfWhileStatement();

            obj.Flags = data.ReadByteValue();
            obj.Variable = data.ReadNullTerminatedAnsiString();
            obj.Value = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an ElseStatement
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ElseStatement on success, null on error</returns>
        private static ElseStatement ParseElseStatement(Stream data)
        {
            return new ElseStatement();
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x11
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x11 on success, null on error</returns>
        private static ScriptUnknown0x11 ParseUnknown0x11(Stream data)
        {
            var obj = new ScriptUnknown0x11();

            obj.Operand_1 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a CopyLocalFile
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="languageCount">Language counter from the header</param>
        /// <returns>Filled CopyLocalFile on success, null on error</returns>
        private static CopyLocalFile ParseCopyLocalFile(Stream data, int languageCount)
        {
            var obj = new CopyLocalFile();

            obj.Operand_1 = data.ReadByteValue();
            obj.Operand_2 = data.ReadBytes(41);
            obj.Source = data.ReadNullTerminatedAnsiString();
            obj.Operand_4 = data.ReadNullTerminatedAnsiString();

            obj.Description = new string[languageCount];
            for (int i = 0; i < obj.Description.Length; i++)
            {
                obj.Description[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            obj.Destination = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a CustomDialogSet
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled CustomDialogSet on success, null on error</returns>
        private static CustomDialogSet ParseCustomDialogSet(Stream data)
        {
            var obj = new CustomDialogSet();

            obj.DeflateStart = data.ReadUInt32LittleEndian();
            obj.DeflateEnd = data.ReadUInt32LittleEndian();
            obj.InflatedSize = data.ReadUInt32LittleEndian();
            obj.DisplayVariable = data.ReadNullTerminatedAnsiString();
            obj.Name = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a GetSystemInformation
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled GetSystemInformation on success, null on error</returns>
        private static GetSystemInformation ParseGetSystemInformation(Stream data)
        {
            var obj = new GetSystemInformation();

            obj.Flags = data.ReadByteValue();
            obj.Variable = data.ReadNullTerminatedAnsiString();
            obj.Pathname = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a GetTemporaryFilename
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled GetTemporaryFilename on success, null on error</returns>
        private static GetTemporaryFilename ParseGetTemporaryFilename(Stream data)
        {
            var obj = new GetTemporaryFilename();

            obj.Variable = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x17
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x17 on success, null on error</returns>
        private static ScriptUnknown0x17 ParseUnknown0x17(Stream data)
        {
            var obj = new ScriptUnknown0x17();

            obj.Operand_1 = data.ReadByteValue();
            obj.Operand_2 = data.ReadBytes(4);
            obj.Operand_3 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Skip the correct amount of data for 0x18
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="op0x18skip">Current 0x18 skip value</param>
        /// <returns>New 0x18 skip value</returns>
        internal static int ParseNewEvent(Stream data, int op0x18skip)
        {
            // If the end of the stream has been reached
            if (data.Position >= data.Length)
                return -1;

            // If the skip amount needs to be determined
            if (op0x18skip == -1)
            {
                byte nextByte = data.ReadByteValue();
                data.Seek(-1, SeekOrigin.Current);

                op0x18skip = nextByte == 0 || nextByte == 0xFF ? 6 : 0;
            }

            // Skip additional bytes
            if (op0x18skip > 0)
                _ = data.ReadBytes(op0x18skip);

            return op0x18skip;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x19
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x19 on success, null on error</returns>
        private static ScriptUnknown0x19 ParseUnknown0x19(Stream data)
        {
            var obj = new ScriptUnknown0x19();

            obj.Operand_1 = data.ReadByteValue();
            obj.Operand_2 = data.ReadNullTerminatedAnsiString();
            obj.Operand_3 = data.ReadNullTerminatedAnsiString();
            obj.Operand_4 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x1A
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x1A on success, null on error</returns>
        private static ScriptUnknown0x1A ParseUnknown0x1A(Stream data)
        {
            var obj = new ScriptUnknown0x1A();

            obj.Operand_1 = data.ReadByteValue();
            obj.Operand_2 = data.ReadNullTerminatedAnsiString();
            obj.Operand_3 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an IncludeScript
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled IncludeScript on success, null on error</returns>
        private static IncludeScript ParseIncludeScript(Stream data)
        {
            return new IncludeScript();
        }

        /// <summary>
        /// Parse a Stream into a AddTextToInstallLog
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled AddTextToInstallLog on success, null on error</returns>
        private static AddTextToInstallLog ParseAddTextToInstallLog(Stream data)
        {
            var obj = new AddTextToInstallLog();

            obj.Text = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x1D
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x1D on success, null on error</returns>
        private static ScriptUnknown0x1D ParseUnknown0x1D(Stream data)
        {
            var obj = new ScriptUnknown0x1D();

            obj.Operand_1 = data.ReadNullTerminatedAnsiString();
            obj.Operand_2 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a CompilerVariableIf
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled CompilerVariableIf on success, null on error</returns>
        private static CompilerVariableIf ParseCompilerVariableIf(Stream data)
        {
            var obj = new CompilerVariableIf();

            obj.Flags = data.ReadByteValue();
            obj.Variable = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptElseIf
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptElseIf on success, null on error</returns>
        private static ElseIfStatement ParseScriptElseIf(Stream data)
        {
            var obj = new ElseIfStatement();

            obj.Operator = data.ReadByteValue();
            obj.Variable = data.ReadNullTerminatedAnsiString();
            obj.Value = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x30
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x30 on success, null on error</returns>
        private static ScriptUnknown0x30 ParseUnknown0x30(Stream data)
        {
            var obj = new ScriptUnknown0x30();

            obj.Operand_1 = data.ReadByteValue();
            obj.Operand_2 = data.ReadNullTerminatedAnsiString();
            obj.Operand_3 = data.ReadNullTerminatedAnsiString();

            return obj;
        }
    }
}
