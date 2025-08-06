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

                var states = ParseStateMachine(data, header);
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

            header.Flags = data.ReadByteValue();
            header.UnknownBytes_1 = data.ReadBytes(4);
            header.SomeOffset1 = data.ReadUInt32LittleEndian();
            header.SomeOffset2 = data.ReadUInt32LittleEndian();
            header.UnknownBytes_2 = data.ReadBytes(4);
            header.DateTime = data.ReadUInt32LittleEndian();
            header.Unknown_22 = data.ReadBytes(22);
            header.FTPURL = data.ReadNullTerminatedAnsiString();
            header.LogPathname = data.ReadNullTerminatedAnsiString();
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
                header.Flags = data.ReadByteValue();
                header.Unknown_22 = data.ReadBytes(17);
                header.FTPURL = data.ReadNullTerminatedAnsiString();
                header.LogPathname = data.ReadNullTerminatedAnsiString();
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

                // Try to handle invalid string lengths
                if (header.ScriptStrings[i].Length > 0)
                {
                    string str = header.ScriptStrings[i];
                    char firstChar = str[0];

                    // Control code blocks
                    bool controlChar = false;
                    if (firstChar < (char)0x0A)
                        controlChar = true;
                    else if (firstChar == (char)0x0A && str.Length == 1)
                        controlChar = true;
                    else if (firstChar > (char)0x0A && firstChar < (char)0x0D)
                        controlChar = true;
                    else if (firstChar == (char)0x0D && str.Length == 1)
                        controlChar = true;
                    else if (firstChar > (char)0x0D && firstChar < (char)0x20)
                        controlChar = true;

                    // Rewind so state can be parsed
                    if (controlChar)
                    {
                        header.ScriptStrings[i] = string.Empty;
                        data.Seek(-str.Length - 1, SeekOrigin.Current);
                        break;
                    }
                }
            }

            return header;
        }

        /// <summary>
        /// Parse a Stream into a state machine
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="header">Parsed script header for information</param>
        /// <param name="old">Indicates an old install script</param>
        /// <returns>Filled state machine on success, null on error</returns>
        private static MachineState[]? ParseStateMachine(Stream data, ScriptHeader header)
        {
            // Extract required information
            byte languageCount = header.LanguageCount;
            bool longDataValue = header.Unknown_22?[1] == 0x10;
            bool old = header.Unknown_22?.Length != 22;

            // Initialize important loop information
            int op0x18skip = -1;

            // Store all states in order
            List<MachineState> states = [];

            while (data.Position < data.Length)
            {
                var op = (OperationCode)data.ReadByteValue();
                MachineStateData? stateData = op switch
                {
                    OperationCode.InstallFile => ParseInstallFile(data, languageCount),
                    OperationCode.Unknown0x01 => null, // No information known, empty?
                    OperationCode.Unknown0x02 => null, // No information known
                    OperationCode.DisplayMessage => ParseDisplayMessage(data, languageCount),
                    OperationCode.UserDefinedActionStep => ParseUserDefinedActionStep(data, languageCount),
                    OperationCode.EditIniFile => ParseEditIniFile(data),
                    OperationCode.UnknownDeflatedFile0x06 => ParseUnknown0x06(data, languageCount, old),
                    OperationCode.ExecuteProgram => ParseExecuteProgram(data),
                    OperationCode.EndBlock => ParseEndBlockStatement(data),
                    OperationCode.CallDllFunction => ParseCallDllFunction(data, languageCount, old),
                    OperationCode.EditRegistry => ParseEditRegistry(data, longDataValue),
                    OperationCode.DeleteFile => ParseDeleteFile(data),
                    OperationCode.IfWhileStatement => ParseIfWhileStatement(data),
                    OperationCode.ElseStatement => ParseElseStatement(data),
                    OperationCode.StartUserDefinedAction => ParseStartUserDefinedAction(data),
                    OperationCode.EndUserDefinedAction => ParseEndUserDefinedAction(data),
                    OperationCode.IgnoreOutputFiles => ParseIgnoreOutputFiles(data),
                    OperationCode.CopyLocalFile => ParseCopyLocalFile(data, languageCount),
                    OperationCode.CustomDialogSet => ParseCustomDialogSet(data),
                    OperationCode.GetSystemInformation => ParseGetSystemInformation(data),
                    OperationCode.GetTemporaryFilename => ParseGetTemporaryFilename(data),
                    OperationCode.PlayMultimediaFile => ParsePlayMultimediaFile(data),
                    OperationCode.NewEvent => ParseNewEvent(data, ref op0x18skip),
                    OperationCode.Unknown0x19 => ParseUnknown0x19(data),
                    OperationCode.ConfigODBCDataSource => ParseConfigODBCDataSource(data),
                    OperationCode.IncludeScript => ParseIncludeScript(data),
                    OperationCode.AddTextToInstallLog => ParseAddTextToInstallLog(data),
                    OperationCode.RenameFileDirectory => ParseRenameFileDirectory(data),
                    OperationCode.OpenCloseInstallLog => ParseOpenCloseInstallLog(data),
                    OperationCode.ElseIfStatement => ParseElseIfStatement(data),

                    //_ => null,
                    _ => throw new IndexOutOfRangeException(nameof(op)),
                };

                // Debug statement
                if (stateData == null)
                    Console.WriteLine($"Opcode {op} resulted in null data");

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
        /// Parse a Stream into a InstallFile
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled InstallFile on success, null on error</returns>
        private static InstallFile ParseInstallFile(Stream data, int languageCount)
        {
            var header = new InstallFile();

            header.Flags = data.ReadUInt16LittleEndian();
            header.DeflateStart = data.ReadUInt32LittleEndian();
            header.DeflateEnd = data.ReadUInt32LittleEndian();
            header.Date = data.ReadUInt16LittleEndian();
            header.Time = data.ReadUInt16LittleEndian();
            header.InflatedSize = data.ReadUInt32LittleEndian();
            header.Operand_7 = data.ReadBytes(20);
            header.Crc32 = data.ReadUInt32LittleEndian();
            header.DestinationPathname = data.ReadNullTerminatedAnsiString();

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
        /// Parse a Stream into a UserDefinedActionStep
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="languageCount">Language counter from the header</param>
        /// <returns>Filled UserDefinedActionStep on success, null on error</returns>
        private static UserDefinedActionStep ParseUserDefinedActionStep(Stream data, int languageCount)
        {
            var obj = new UserDefinedActionStep();

            obj.Count = data.ReadByteValue();
            obj.ScriptLines = new string[languageCount];
            for (int i = 0; i < obj.ScriptLines.Length; i++)
            {
                obj.ScriptLines[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
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
        /// <param name="old">Indicates an old install script</param>
        /// <returns>Filled ScriptUnknown0x06 on success, null on error</returns>
        private static ScriptUnknown0x06 ParseUnknown0x06(Stream data, int languageCount, bool old)
        {
            var obj = new ScriptUnknown0x06();

            obj.Operand_1 = data.ReadBytes(2);
            obj.Operand_2 = data.ReadUInt32LittleEndian();
            obj.DeflateInfo = ParseScriptDeflateInfoContainer(data, languageCount);

            // Terminator byte does not exist in old scripts
            if (!old)
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
        /// Parse a Stream into a CallDllFunction
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="languageCount">Language counter from the header</param>
        /// <param name="old">Indicates an old install script</param>
        /// <returns>Filled CallDllFunction on success, null on error</returns>
        private static CallDllFunction ParseCallDllFunction(Stream data, int languageCount, bool old)
        {
            var obj = new CallDllFunction();

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
                string[] parts = obj.Entries[i].Split((char)0x7F);

                // Switch based on the function
                // TODO: Remove after mapping is complete
                switch (obj.FunctionName)
                {
                    // Unknown
                    case "f1":
                        // TODO: Implement
                        // Probably this layout: 
                        // - Flags (numeric) (e.g. "12", "8")
                        // - Unknown string (empty in samples)
                        // - Executable path (e.g. "%WIN%\hcwSubID.exe", "765.exe")
                        // - Executable path again (e.g. "%WIN%\hcwSubID.exe", "765.exe")
                        // - Unknown string (empty in samples)
                        // - Numeric value (e.g. "0")
                        break;

                    // Add to SYSTEM.INI
                    case "f3": break;

                    // Read INI Value
                    case "f8": break;

                    // Get Registry Key Value
                    case "f9": break;

                    // Register Font
                    case "f10": break;

                    // Win32 System Directory
                    case "f11": break;

                    // Check Configuration
                    case "f12": break;

                    // Search for File
                    case "f13": break;

                    // Read/Write Binary File
                    case "f15": break;

                    // Set Variable
                    case "f16": break;

                    // Get Environment Variable
                    case "f17": break;

                    // Check if File/Dir Exists
                    case "f19": break;

                    // Set File Attributes
                    case "f20": break;

                    // Find File in Path
                    case "f22": break;

                    // Check Disk Space
                    case "f23": break;

                    // Insert Line Into Text File
                    case "f25": break;

                    // Parse String
                    case "f27": break;

                    // Unknown
                    case "f28":
                        // TODO: Implement
                        // Probably this layout:
                        // - Unknown string (empty in samples)
                        break;

                    // Self-Register OCXs/DLLs
                    case "f29": break;

                    // Unknown
                    case "f30":
                        // TODO: Implement
                        // Maybe "Modify Component Size"?
                        // Probably this layout:
                        // - Flags (numeric)
                        // - Directory name (e.g. "%INST%\..\DirectX")
                        // - File name (e.g. "%INST%\..\DirectX\DSETUP.DLL")
                        // - Size? (e.g. "2623")
                        break;

                    // Wizard Block
                    case "f31": break;

                    // Read/Update Text File
                    case "f33": break;

                    // Post to HTTP Server
                    case "f34": break;

                    // Prompt for Filename 
                    case "f35": break;

                    // Start/Stop Service
                    case "f36": break;

                    // External DLL Calls
                    default:
                        if (string.IsNullOrEmpty(obj.DllPath))
                            Console.WriteLine($"Unrecognized function: {obj.FunctionName} with parts: {string.Join(", ", parts)}");

                        break;
                }
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a EditRegistry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="longDataValue">Indicates an old install script</param>
        /// <returns>Filled EditRegistry on success, null on error</returns>
        private static EditRegistry ParseEditRegistry(Stream data, bool longDataValue)
        {
            // Cache the current offset
            long current = data.Position;

            // Read as standard first
            var obj = new EditRegistry();

            obj.Root = data.ReadByteValue();

            if (longDataValue)
                obj.DataType = data.ReadUInt16();
            else
                obj.DataType = data.ReadByteValue();

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
        /// Parse a Stream into an ElseStatement
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled StartUserDefinedAction on success, null on error</returns>
        private static StartUserDefinedAction ParseStartUserDefinedAction(Stream data)
        {
            return new StartUserDefinedAction();
        }

        /// <summary>
        /// Parse a Stream into an EndUserDefinedAction
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled EndUserDefinedAction on success, null on error</returns>
        private static EndUserDefinedAction ParseEndUserDefinedAction(Stream data)
        {
            return new EndUserDefinedAction();
        }

        /// <summary>
        /// Parse a Stream into a IgnoreOutputFiles
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled IgnoreOutputFiles on success, null on error</returns>
        private static IgnoreOutputFiles ParseIgnoreOutputFiles(Stream data)
        {
            var obj = new IgnoreOutputFiles();

            obj.Pathname = data.ReadNullTerminatedAnsiString();

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
        /// Parse a Stream into a PlayMultimediaFile
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled PlayMultimediaFile on success, null on error</returns>
        private static PlayMultimediaFile ParsePlayMultimediaFile(Stream data)
        {
            var obj = new PlayMultimediaFile();

            obj.Flags = data.ReadByteValue();
            obj.XPosition = data.ReadUInt16LittleEndian();
            obj.YPosition = data.ReadUInt16LittleEndian();
            obj.Pathname = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a NewEvent
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="op0x18skip">Current 0x18 skip value</param>
        /// <returns>Filled NewEvent on success, null on error</returns>
        internal static NewEvent ParseNewEvent(Stream data, ref int op0x18skip)
        {
            // If the end of the stream has been reached
            if (data.Position >= data.Length)
                return new NewEvent();

            // If the skip amount needs to be determined
            if (op0x18skip == -1)
            {
                byte nextByte = data.ReadByteValue();
                data.Seek(-1, SeekOrigin.Current);

                op0x18skip = nextByte == 0 || nextByte == 0xFF ? 6 : 0;
            }

            var obj = new NewEvent();

            // Skip additional bytes
            if (op0x18skip > 0)
                obj.Padding = data.ReadBytes(op0x18skip);

            return obj;
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
        /// Parse a Stream into a ConfigODBCDataSource
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ConfigODBCDataSource on success, null on error</returns>
        private static ConfigODBCDataSource ParseConfigODBCDataSource(Stream data)
        {
            var obj = new ConfigODBCDataSource();

            obj.Flags = data.ReadByteValue();
            obj.FileFormat = data.ReadNullTerminatedAnsiString();
            obj.ConnectionString = data.ReadNullTerminatedAnsiString();

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
        /// Parse a Stream into a RenameFileDirectory
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled RenameFileDirectory on success, null on error</returns>
        private static RenameFileDirectory ParseRenameFileDirectory(Stream data)
        {
            var obj = new RenameFileDirectory();

            obj.OldPathname = data.ReadNullTerminatedAnsiString();
            obj.NewFileName = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a OpenCloseInstallLog
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled OpenCloseInstallLog on success, null on error</returns>
        private static OpenCloseInstallLog ParseOpenCloseInstallLog(Stream data)
        {
            var obj = new OpenCloseInstallLog();

            obj.Flags = data.ReadByteValue();
            obj.LogName = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ElseIfStatement
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ElseIfStatement on success, null on error</returns>
        private static ElseIfStatement ParseElseIfStatement(Stream data)
        {
            var obj = new ElseIfStatement();

            obj.Operator = data.ReadByteValue();
            obj.Variable = data.ReadNullTerminatedAnsiString();
            obj.Value = data.ReadNullTerminatedAnsiString();

            return obj;
        }
    }
}
