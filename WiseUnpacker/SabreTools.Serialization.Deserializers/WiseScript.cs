using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.Models.GCF;
using SabreTools.Models.WiseInstaller;

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

                var states = ParseStateMachine(data, header.LanguageCount);
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

            header.FileTexts = new string[languageCount];
            for (int i = 0; i < header.FileTexts.Length; i++)
            {
                header.FileTexts[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            header.Operand_11 = data.ReadNullTerminatedAnsiString();

            return header;
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
            if (header.MessageFont != null && header.MessageFont.Length == 0)
            {
                // Seek back to the original position
                data.Seek(current, SeekOrigin.Begin);

                // Recreate the header with minimal data
                header = new ScriptHeader();

                // TODO: Figure out if this maps to existing fields
                header.Unknown_22 = data.ReadBytes(20);
                header.MessageFont = data.ReadNullTerminatedAnsiString();
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

            header.ScriptStrings = new string[55 * header.LanguageCount];
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
        /// <returns>Filled state machine on success, null on error</returns>
        private static MachineState[]? ParseStateMachine(Stream data, byte languageCount)
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
                    OperationCode.Unknown0x03 => ParseUnknown0x03(data, languageCount),
                    OperationCode.FormData => ParseScriptFormData(data, languageCount),
                    OperationCode.EditIniFile => ParseScriptEditIniFile(data),
                    OperationCode.UnknownDeflatedFile0x06 => ParseUnknown0x06(data, languageCount),
                    OperationCode.ExecuteProgram => ParseScriptExecuteProgram(data),
                    OperationCode.EndBlock => ParseScriptEndBlock(data),
                    OperationCode.FunctionCall => ParseScriptFunctionCall(data, languageCount),
                    OperationCode.EditRegistry => ParseScriptEditRegistry(data),
                    OperationCode.DeleteFile => ParseScriptDeleteFile(data),
                    OperationCode.IfWhileStatement => ParseScriptIfWhileStatement(data),
                    OperationCode.ElseStatement => null, // No-op
                    OperationCode.StartFormData => null, // No-op
                    OperationCode.EndFormData => null, // No-op
                    OperationCode.Unknown0x11 => ParseUnknown0x11(data),
                    OperationCode.FileOnInstallMedium => ParseUnknown0x12(data, languageCount),
                    OperationCode.CustomDialogSet => ParseScriptCustomDialogSet(data),
                    OperationCode.GetSystemInformation => ParseScriptGetSystemInformation(data),
                    OperationCode.GetTemporaryFilename => ParseScriptGetTemporaryFilename(data),
                    OperationCode.Unknown0x17 => ParseUnknown0x17(data),
                    OperationCode.Skip0x18 => null, // No-op, handled below
                    OperationCode.Unknown0x19 => ParseUnknown0x19(data),
                    OperationCode.Unknown0x1A => ParseUnknown0x1A(data),
                    OperationCode.IncludeScript => null, // No-op
                    OperationCode.AddTextToInstallLog => ParseScriptAddTextToInstallLog(data),
                    OperationCode.Unknown0x1D => ParseUnknown0x1D(data),
                    OperationCode.CompilerVariableIf => ParseScriptCompilerVariableIf(data),
                    OperationCode.ElseIfStatement => ParseUnknown0x23(data),
                    OperationCode.Skip0x24 => null, // No-op
                    OperationCode.Skip0x25 => null, // No-op
                    OperationCode.ReadByteAndStrings => ParseUnknown0x30(data),

                    _ => throw new IndexOutOfRangeException(nameof(op)),
                };

                // Special handling
                if (op == OperationCode.Skip0x18)
                    op0x18skip = ParseUnknown0x18(data, op0x18skip);

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
        /// Parse a Stream into a ScriptUnknown0x03
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x03 on success, null on error</returns>
        private static ScriptUnknown0x03 ParseUnknown0x03(Stream data, int languageCount)
        {
            var obj = new ScriptUnknown0x03();

            obj.Operand_0 = data.ReadByteValue();
            obj.LangStrings = new string[languageCount * 2];
            for (int i = 0; i < obj.LangStrings.Length; i++)
            {
                obj.LangStrings[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptFormData
        /// </summary>
        /// <param name="data">Stream to parse</param>
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
        /// Parse a Stream into a ScriptEditIniFile
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptEditIniFile on success, null on error</returns>
        private static ScriptEditIniFile ParseScriptEditIniFile(Stream data)
        {
            var obj = new ScriptEditIniFile();

            obj.Pathname = data.ReadNullTerminatedAnsiString();
            obj.Section = data.ReadNullTerminatedAnsiString();
            obj.Values = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x06
        /// </summary>
        /// <param name="data">Stream to parse</param>
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
        /// Parse a Stream into a ScriptExecuteProgram
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptExecuteProgram on success, null on error</returns>
        private static ScriptExecuteProgram ParseScriptExecuteProgram(Stream data)
        {
            var obj = new ScriptExecuteProgram();

            obj.Flags = data.ReadByteValue();
            obj.Pathname = data.ReadNullTerminatedAnsiString();
            obj.CommandLine = data.ReadNullTerminatedAnsiString();
            obj.DefaultDirectory = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptEndBlock
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptEndBlock on success, null on error</returns>
        private static ScriptEndBlock ParseScriptEndBlock(Stream data)
        {
            var obj = new ScriptEndBlock();

            obj.Operand_1 = data.ReadByteValue();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptFunctionCall
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptFunctionCall on success, null on error</returns>
        private static ScriptFunctionCall ParseScriptFunctionCall(Stream data, int languageCount)
        {
            var obj = new ScriptFunctionCall();

            obj.Operand_1 = data.ReadByteValue();
            obj.DllPath = data.ReadNullTerminatedAnsiString();
            obj.FunctionName = data.ReadNullTerminatedAnsiString();
            obj.Operand_4 = data.ReadNullTerminatedAnsiString();
            obj.ReturnVariable = data.ReadNullTerminatedAnsiString();

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

                    // Unknown external
                    case "f13":
                        // TODO: Implement
                        // Possibly Firefox-related for cookies?
                        // Probably this layout:
                        // - Variable for the dir name? (e.g. "DC_FIREFOX_COOKIE_DIR")
                        // - Search path for the dir
                        // - Variable name/message for not found directory (e.g. "NODIRFOUNDBAKA")
                        break;

                    // Set Variable
                    case "f16": break;

                    // Get Environment Variable
                    case "f17": break;

                    // Check if File/Dir Exists
                    case "f19": break;

                    // Unknown external
                    case "f23":
                        // TODO: Implement
                        // Posssibly an uninstall creation script?
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

                    // Unknown external
                    case "f33":
                        // TODO: Implement
                        // Possibly reading or writing from a cookies file
                        // Probably this layout:
                        // - Variable to read to/write from (e.g. "DC_LINE_OF_TEXT")
                        // - Path to the cookies file (e.g. "%DC_WIN_DRIVE%:\Documents and Settings\%DC_LOGON_NAME%\Cookies\%DC_LOGON_NAME%@%DC_COOKIE_DOMAIN%[1].txt")
                        // - Unknown string; in samples it was all 0x20-filled
                        break;

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
        private static ScriptEditRegistry ParseScriptEditRegistry(Stream data)
        {
            var obj = new ScriptEditRegistry();

            obj.Root = data.ReadByteValue();
            obj.DataType = data.ReadByteValue();
            obj.Operand_3 = data.ReadByteValue();
            obj.Key = data.ReadNullTerminatedAnsiString();
            obj.NewValue = data.ReadNullTerminatedAnsiString();
            obj.ValueName = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptDeleteFile
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptDeleteFile on success, null on error</returns>
        private static ScriptDeleteFile ParseScriptDeleteFile(Stream data)
        {
            var obj = new ScriptDeleteFile();

            obj.Flags = data.ReadByteValue();
            obj.Pathname = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptIfWhileStatement
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptIfWhileStatement on success, null on error</returns>
        private static ScriptIfWhileStatement ParseScriptIfWhileStatement(Stream data)
        {
            var obj = new ScriptIfWhileStatement();

            obj.Flags = data.ReadByteValue();
            obj.Variable = data.ReadNullTerminatedAnsiString();
            obj.Value = data.ReadNullTerminatedAnsiString();

            return obj;
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
        /// Parse a Stream into a ScriptUnknown0x12
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x12 on success, null on error</returns>
        private static ScriptUnknown0x12 ParseUnknown0x12(Stream data, int languageCount)
        {
            var obj = new ScriptUnknown0x12();

            obj.Operand_1 = data.ReadByteValue();
            obj.Operand_2 = data.ReadBytes(41);
            obj.SourceFile = data.ReadNullTerminatedAnsiString();
            obj.Operand_4 = data.ReadNullTerminatedAnsiString();

            obj.Operand_5 = new string[languageCount];
            for (int i = 0; i < obj.Operand_5.Length; i++)
            {
                obj.Operand_5[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            obj.DestFile = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptCustomDialogSet
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptCustomDialogSet on success, null on error</returns>
        private static ScriptCustomDialogSet ParseScriptCustomDialogSet(Stream data)
        {
            var obj = new ScriptCustomDialogSet();

            obj.DeflateStart = data.ReadUInt32LittleEndian();
            obj.DeflateEnd = data.ReadUInt32LittleEndian();
            obj.InflatedSize = data.ReadUInt32LittleEndian();
            obj.DisplayVariable = data.ReadNullTerminatedAnsiString();
            obj.Name = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptGetSystemInformation
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptGetSystemInformation on success, null on error</returns>
        private static ScriptGetSystemInformation ParseScriptGetSystemInformation(Stream data)
        {
            var obj = new ScriptGetSystemInformation();

            obj.Flags = data.ReadByteValue();
            obj.Variable = data.ReadNullTerminatedAnsiString();
            obj.Operand_3 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptGetTemporaryFilename
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptGetTemporaryFilename on success, null on error</returns>
        private static ScriptGetTemporaryFilename ParseScriptGetTemporaryFilename(Stream data)
        {
            var obj = new ScriptGetTemporaryFilename();

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
        internal static int ParseUnknown0x18(Stream data, int op0x18skip)
        {
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
        /// Parse a Stream into a ScriptAddTextToInstallLog
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptAddTextToInstallLog on success, null on error</returns>
        private static ScriptAddTextToInstallLog ParseScriptAddTextToInstallLog(Stream data)
        {
            var obj = new ScriptAddTextToInstallLog();

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
        /// Parse a Stream into a ScriptCompilerVariableIf
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptCompilerVariableIf on success, null on error</returns>
        private static ScriptCompilerVariableIf ParseScriptCompilerVariableIf(Stream data)
        {
            var obj = new ScriptCompilerVariableIf();

            obj.Flags = data.ReadByteValue();
            obj.Variable = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x23
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x23 on success, null on error</returns>
        private static ScriptUnknown0x23 ParseUnknown0x23(Stream data)
        {
            var obj = new ScriptUnknown0x23();

            obj.Operand_1 = data.ReadByteValue();
            obj.VarName = data.ReadNullTerminatedAnsiString();
            obj.VarValue = data.ReadNullTerminatedAnsiString();

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
