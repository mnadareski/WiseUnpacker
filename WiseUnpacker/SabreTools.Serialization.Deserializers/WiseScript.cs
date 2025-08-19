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
                // Cache the current offset
                long initialOffset = data.Position;

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
        /// Parse a Stream into a ScriptHeader
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptHeader on success, null on error</returns>
        private static ScriptHeader ParseScriptHeader(Stream data)
        {
            // Cache the current position in case of a trimmed header
            long current = data.Position;

            var header = new ScriptHeader();

            // Attempt to read strings at 0x12 (Short)
            data.Seek(current + 0x12, SeekOrigin.Begin);
            string? ftpUrl = data.ReadNullTerminatedAnsiString();
            string? logPath = data.ReadNullTerminatedAnsiString();
            string? messageFont = data.ReadNullTerminatedAnsiString();
            data.Seek(current, SeekOrigin.Begin);

            // If the strings are valid
            if ((ftpUrl != null && (ftpUrl.Length == 0 || ftpUrl.Split('.').Length > 2))
                && (logPath != null && (logPath.Length == 0 || logPath.StartsWith("%")))
                && (messageFont != null && (messageFont.Length == 0 || !IsTypicalControlCode(messageFont, strict: true)))
                && !(ftpUrl.Length == 0 && logPath.Length == 0 && messageFont.Length == 0))
            {
                // TODO: Figure out if this maps to existing fields
                header.Flags = data.ReadByteValue();
                header.UnknownU16_1 = data.ReadUInt16LittleEndian();
                header.UnknownU16_2 = data.ReadUInt16LittleEndian();
                header.DateTime = data.ReadUInt32LittleEndian();
                header.VariableLengthData = data.ReadBytes(9);

                goto ReadStrings;
            }

            // Attempt to read strings at 0x26 (Middle)
            data.Seek(current + 0x26, SeekOrigin.Begin);
            ftpUrl = data.ReadNullTerminatedAnsiString();
            logPath = data.ReadNullTerminatedAnsiString();
            messageFont = data.ReadNullTerminatedAnsiString();
            data.Seek(current, SeekOrigin.Begin);

            // If the strings are valid
            if ((ftpUrl != null && (ftpUrl.Length == 0 || ftpUrl.Split('.').Length > 2))
                && (logPath != null && (logPath.Length == 0 || logPath.StartsWith("%")))
                && (messageFont != null && (messageFont.Length == 0 || !IsTypicalControlCode(messageFont, strict: true)))
                && !(ftpUrl.Length == 0 && logPath.Length == 0 && messageFont.Length == 0))
            {
                header.Flags = data.ReadByteValue();
                header.UnknownU16_1 = data.ReadUInt16LittleEndian();
                header.UnknownU16_2 = data.ReadUInt16LittleEndian();
                header.SomeOffset1 = data.ReadUInt32LittleEndian();
                header.SomeOffset2 = data.ReadUInt32LittleEndian();
                header.UnknownBytes_2 = data.ReadBytes(4);
                header.DateTime = data.ReadUInt32LittleEndian();
                header.VariableLengthData = data.ReadBytes(17);

                goto ReadStrings;
            }

            // Attempt to read strings at 0x34 (Long)
            data.Seek(current + 0x34, SeekOrigin.Begin);
            ftpUrl = data.ReadNullTerminatedAnsiString();
            logPath = data.ReadNullTerminatedAnsiString();
            messageFont = data.ReadNullTerminatedAnsiString();
            data.Seek(current, SeekOrigin.Begin);

            // If the strings are valid
            if ((ftpUrl != null && (ftpUrl.Length == 0 || ftpUrl.Split('.').Length > 2))
                && (logPath != null && (logPath.Length == 0 || logPath.StartsWith("%")))
                && (messageFont != null && (messageFont.Length == 0 || !IsTypicalControlCode(messageFont, strict: true)))
                && !(ftpUrl.Length == 0 && logPath.Length == 0 && messageFont.Length == 0))
            {
                header.Flags = data.ReadByteValue();
                header.UnknownU16_1 = data.ReadUInt16LittleEndian();
                header.UnknownU16_2 = data.ReadUInt16LittleEndian();
                header.SomeOffset1 = data.ReadUInt32LittleEndian();
                header.SomeOffset2 = data.ReadUInt32LittleEndian();
                header.UnknownBytes_2 = data.ReadBytes(4);
                header.DateTime = data.ReadUInt32LittleEndian();
                header.VariableLengthData = data.ReadBytes(31);

                goto ReadStrings;
            }

            // Otherwise, assume a standard header (Normal)
            header.Flags = data.ReadByteValue();
            header.UnknownU16_1 = data.ReadUInt16LittleEndian();
            header.UnknownU16_2 = data.ReadUInt16LittleEndian();
            header.SomeOffset1 = data.ReadUInt32LittleEndian();
            header.SomeOffset2 = data.ReadUInt32LittleEndian();
            header.UnknownBytes_2 = data.ReadBytes(4);
            header.DateTime = data.ReadUInt32LittleEndian();
            header.VariableLengthData = data.ReadBytes(22);

        ReadStrings:
            header.FTPURL = data.ReadNullTerminatedAnsiString();
            header.LogPathname = data.ReadNullTerminatedAnsiString();
            header.MessageFont = data.ReadNullTerminatedAnsiString();
            header.FontSize = data.ReadUInt32LittleEndian();
            header.Unknown_2 = data.ReadBytes(2);
            header.LanguageCount = data.ReadByteValue();

            List<string> headerStrings = [];
            while (true)
            {
                string? str = data.ReadNullTerminatedAnsiString();
                if (str == null)
                    break;

                // Try to handle invalid string lengths
                if (str.Length > 0 && IsTypicalControlCode(str, strict: false))
                {
                    data.Seek(-str.Length - 1, SeekOrigin.Current);
                    break;
                }

                // Try to handle InstallFile calls
                long original = data.Position;
                if (str.Length == 0)
                {
                    data.Seek(-1, SeekOrigin.Current);

                    // Try to read the next block as an install file call
                    var maybeInstall = ParseInstallFile(data, header.LanguageCount);
                    if (maybeInstall != null
                        && (maybeInstall.DeflateEnd - maybeInstall.DeflateStart) < data.Length
                        && (maybeInstall.DeflateEnd - maybeInstall.DeflateStart) < maybeInstall.InflatedSize)
                    {
                        data.Seek(original - 1, SeekOrigin.Begin);
                        break;
                    }

                    // Otherwise, seek back to reading
                    data.Seek(original, SeekOrigin.Begin);
                }

                headerStrings.Add(str);
            }

            header.HeaderStrings = [.. headerStrings];

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
            bool shortDllCall = header.VariableLengthData?.Length != 22
                && header.SomeOffset1 == 0x00000000
                && header.Flags != 0x0008
                && header.Flags != 0x0014;

            // Initialize important loop information
            int op0x18skip = -1;
            bool? registryDll = null;
            bool switched = false;

            // Store the start of the state machine
            long machineStart = data.Position;

            // Store all states in order
            List<MachineState> states = [];

            while (data.Position < data.Length)
            {
                var op = (OperationCode)data.ReadByteValue();
                MachineStateData? stateData = op switch
                {
                    OperationCode.InstallFile => ParseInstallFile(data, languageCount),
                    OperationCode.Invalid0x01 => ParseInvalidOperation(data),
                    OperationCode.NoOp => ParseNoOp(data),
                    OperationCode.DisplayMessage => ParseDisplayMessage(data, languageCount),
                    OperationCode.UserDefinedActionStep => ParseUserDefinedActionStep(data, languageCount),
                    OperationCode.EditIniFile => ParseEditIniFile(data),
                    OperationCode.DisplayBillboard => ParseDisplayBillboard(data, languageCount),
                    OperationCode.ExecuteProgram => ParseExecuteProgram(data),
                    OperationCode.EndBlock => ParseEndBlockStatement(data),
                    OperationCode.CallDllFunction => ParseCallDllFunction(data, languageCount, shortDllCall),
                    OperationCode.EditRegistry => ParseEditRegistry(data, ref registryDll),
                    OperationCode.DeleteFile => ParseDeleteFile(data),
                    OperationCode.IfWhileStatement => ParseIfWhileStatement(data),
                    OperationCode.ElseStatement => ParseElseStatement(data),
                    OperationCode.Invalid0x0E => ParseInvalidOperation(data),
                    OperationCode.StartUserDefinedAction => ParseStartUserDefinedAction(data),
                    OperationCode.EndUserDefinedAction => ParseEndUserDefinedAction(data),
                    OperationCode.CreateDirectory => ParseCreateDirectory(data),
                    OperationCode.CopyLocalFile => ParseCopyLocalFile(data, languageCount),
                    OperationCode.Invalid0x13 => ParseInvalidOperation(data),
                    OperationCode.CustomDialogSet => ParseCustomDialogSet(data),
                    OperationCode.GetSystemInformation => ParseGetSystemInformation(data),
                    OperationCode.GetTemporaryFilename => ParseGetTemporaryFilename(data),
                    OperationCode.PlayMultimediaFile => ParsePlayMultimediaFile(data),
                    OperationCode.NewEvent => ParseNewEvent(data, languageCount, shortDllCall, ref op0x18skip),
                    OperationCode.InstallODBCDriver => ParseUnknown0x19(data),
                    OperationCode.ConfigODBCDataSource => ParseConfigODBCDataSource(data),
                    OperationCode.IncludeScript => ParseIncludeScript(data),
                    OperationCode.AddTextToInstallLog => ParseAddTextToInstallLog(data),
                    OperationCode.RenameFileDirectory => ParseRenameFileDirectory(data),
                    OperationCode.OpenCloseInstallLog => ParseOpenCloseInstallLog(data),
                    OperationCode.Invalid0x1F => ParseInvalidOperation(data),
                    OperationCode.Invalid0x20 => ParseInvalidOperation(data),
                    OperationCode.Invalid0x21 => ParseInvalidOperation(data),
                    OperationCode.Invalid0x22 => ParseInvalidOperation(data),
                    OperationCode.ElseIfStatement => ParseElseIfStatement(data),
                    OperationCode.Unknown0x24 => ParseUnknown0x24(data),
                    OperationCode.Unknown0x25 => ParseUnknown0x25(data),

                    // Handled separately below
                    _ => null,
                };

                // If an error is detected, try parsing with flipped short DLL call values
                if (stateData == null)
                {
                    // If there has already been one switch, don't try again
                    if (switched)
                        throw new IndexOutOfRangeException(nameof(op));

                    // Debug statement
                    Console.WriteLine($"Opcode {op} resulted in null data, trying with alternate values");

                    // Reset the state
                    switched = true;
                    shortDllCall = !shortDllCall;
                    states.Clear();

                    // Seek to the start of the machine and try again
                    data.Seek(machineStart, SeekOrigin.Begin);
                    continue;
                }

                var state = new MachineState
                {
                    Op = op,
                    Data = stateData,
                };
                states.Add(state);
            }

            return [.. states];
        }

        #region State Actions

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

            header.Source = data.ReadNullTerminatedAnsiString();

            return header;
        }

        /// <summary>
        /// Parse a Stream into a NoOp
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled NoOp on success, null on error</returns>
        private static NoOp ParseNoOp(Stream data)
        {
            return new NoOp();
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

            obj.Flags = data.ReadByteValue();
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
        /// Parse a Stream into a DisplayBillboard
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="languageCount">Language counter from the header</param>
        /// <returns>Filled DisplayBillboard on success, null on error</returns>
        private static DisplayBillboard ParseDisplayBillboard(Stream data, int languageCount)
        {
            var obj = new DisplayBillboard();

            obj.Flags = data.ReadUInt16LittleEndian();
            obj.Operand_2 = data.ReadUInt16LittleEndian();
            obj.Operand_3 = data.ReadUInt16LittleEndian();
            obj.DeflateInfo = new DeflateEntry[languageCount];
            for (int i = 0; i < obj.DeflateInfo.Length; i++)
            {
                obj.DeflateInfo[i] = ParseDeflateEntry(data);
            }

            // Check the terminator byte is 0x00
            obj.Terminator = data.ReadByteValue();
            if (obj.Terminator != 0x00)
            {
                obj.Terminator = 0x00;
                data.Seek(-1, SeekOrigin.Current);
            }

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
        /// <param name="shortDllCall">Indicates a short DLL call</param>
        /// <returns>Filled CallDllFunction on success, null on error</returns>
        private static CallDllFunction ParseCallDllFunction(Stream data, int languageCount, bool shortDllCall)
        {
            var obj = new CallDllFunction();

            obj.Flags = data.ReadByteValue();
            obj.DllPath = data.ReadNullTerminatedAnsiString();
            obj.FunctionName = data.ReadNullTerminatedAnsiString();
            if (!shortDllCall)
            {
                obj.Operand_4 = data.ReadNullTerminatedAnsiString();
                obj.ReturnVariable = data.ReadNullTerminatedAnsiString();
            }

            obj.Entries = new FunctionData[languageCount];
            for (int i = 0; i < obj.Entries.Length; i++)
            {
                // Switch based on the function
                string entryString = data.ReadNullTerminatedAnsiString() ?? string.Empty;
                obj.Entries[i] = obj.FunctionName switch
                {
                    "f0" => ParseAddDirectoryToPath(entryString),
                    "f1" => ParseAddToAutoexecBat(entryString),
                    "f2" => ParseAddToConfigSys(entryString),
                    "f3" => ParseAddToSystemIni(entryString),
                    "f8" => ParseReadIniValue(entryString),
                    "f9" => ParseGetRegistryKeyValue(entryString),
                    "f10" => ParseRegisterFont(entryString),
                    "f11" => ParseWin32SystemDirectory(entryString),
                    "f12" => ParseCheckConfiguration(entryString),
                    "f13" => ParseSearchForFile(entryString),
                    "f15" => ParseReadWriteBinaryFile(entryString),
                    "f16" => ParseSetVariable(entryString),
                    "f17" => ParseGetEnvironmentVariable(entryString),
                    "f19" => ParseCheckIfFileDirExists(entryString),
                    "f20" => ParseSetFileAttributes(entryString),
                    "f21" => ParseSetFilesBuffers(entryString),
                    "f22" => ParseFindFileInPath(entryString),
                    "f23" => ParseCheckDiskSpace(entryString),
                    "f25" => ParseInsertLineIntoTextFile(entryString),
                    "f27" => ParseParseString(entryString),
                    "f28" => ParseExitInstallation(entryString),
                    "f29" => ParseSelfRegisterOCXsDLLs(entryString),
                    "f30" => ParseInstallDirectXComponents(entryString),
                    "f31" => ParseWizardBlockLoop(entryString),
                    "f33" => ParseReadUpdateTextFile(entryString),
                    "f34" => ParsePostToHttpServer(entryString),
                    "f35" => ParsePromptForFilename(entryString),
                    "f36" => ParseStartStopService(entryString),
                    "f38" => ParseCheckHttpConnection(entryString),

                    // External and unrecognized functions
                    _ => ParseExternalDllCall(entryString),
                };

                // Log if a truely unknown function is found
                if (obj.Entries[i] is ExternalDllCall edc && string.IsNullOrEmpty(obj.DllPath))
                    Console.WriteLine($"Unrecognized function: {obj.FunctionName} with parts: {string.Join(", ", edc.Args ?? [])}");

            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a EditRegistry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="registryDll">Indicates if the longer value set is used</param>
        /// <returns>Filled EditRegistry on success, null on error</returns>
        private static EditRegistry ParseEditRegistry(Stream data, ref bool? registryDll)
        {
            var obj = new EditRegistry();

            obj.FlagsAndRoot = data.ReadByteValue();

            // If the fsllib32.dll flag is set
            if (registryDll == false)
            {
                obj.DataType = data.ReadByteValue();
                obj.Key = data.ReadNullTerminatedAnsiString();
                obj.NewValue = data.ReadNullTerminatedAnsiString();
                obj.ValueName = data.ReadNullTerminatedAnsiString();
                return obj;
            }
            else if (registryDll == true)
            {
                obj.DataType = data.ReadByteValue();
                obj.UnknownFsllib = data.ReadNullTerminatedAnsiString();
                obj.Key = data.ReadNullTerminatedAnsiString();
                obj.NewValue = data.ReadNullTerminatedAnsiString();
                obj.ValueName = data.ReadNullTerminatedAnsiString();
                return obj;
            }

            // Check for an empty registry call
            uint possiblyEmpty = data.ReadUInt32LittleEndian();
            data.Seek(-4, SeekOrigin.Current);
            if (possiblyEmpty == 0x00000000)
            {
                obj.DataType = data.ReadByteValue();
                obj.Key = data.ReadNullTerminatedAnsiString();
                obj.NewValue = data.ReadNullTerminatedAnsiString();
                obj.ValueName = data.ReadNullTerminatedAnsiString();

                registryDll = false;
                return obj;
            }

            // Assume use until otherwise determined
            registryDll = true;

            obj.DataType = data.ReadByteValue();
            obj.UnknownFsllib = data.ReadNullTerminatedAnsiString();
            obj.Key = data.ReadNullTerminatedAnsiString();
            obj.NewValue = data.ReadNullTerminatedAnsiString();
            obj.ValueName = data.ReadNullTerminatedAnsiString();

            // If the delete pattern is found
            if (obj.UnknownFsllib != null && obj.UnknownFsllib.Length > 0
                && obj.Key != null && obj.Key.Length == 0
                && obj.NewValue != null && obj.NewValue.Length == 0)
            {
                data.Seek(-(obj.ValueName?.Length ?? 0) - 1, SeekOrigin.Current);
                obj.ValueName = obj.NewValue;
                obj.NewValue = obj.Key;
                obj.Key = obj.UnknownFsllib;
                obj.UnknownFsllib = null;
                registryDll = false;
            }

            // If the last value is a control
            else if (obj.ValueName != null && IsTypicalControlCode(obj.ValueName, strict: true))
            {
                data.Seek(-obj.ValueName.Length - 1, SeekOrigin.Current);
                obj.ValueName = obj.NewValue;
                obj.NewValue = obj.Key;
                obj.Key = obj.UnknownFsllib;
                obj.UnknownFsllib = null;
                registryDll = false;
            }

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
        /// Parse a Stream into a CreateDirectory
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled CreateDirectory on success, null on error</returns>
        private static CreateDirectory ParseCreateDirectory(Stream data)
        {
            var obj = new CreateDirectory();

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

            obj.Flags = data.ReadUInt16LittleEndian();
            obj.Padding = data.ReadBytes(40);
            obj.Destination = data.ReadNullTerminatedAnsiString();

            obj.Description = new string[languageCount + 1];
            for (int i = 0; i < obj.Description.Length; i++)
            {
                obj.Description[i] = data.ReadNullTerminatedAnsiString() ?? string.Empty;
            }

            obj.Source = data.ReadNullTerminatedAnsiString();

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
        /// <param name="languageCount">Language counter from the header</param>
        /// <param name="shortDllCall">Indicates a short DLL call</param>
        /// <param name="op0x18skip">Current 0x18 skip value</param>
        /// <returns>Filled NewEvent on success, null on error</returns>
        internal static NewEvent ParseNewEvent(Stream data, int languageCount, bool shortDllCall, ref int op0x18skip)
        {
            // If the end of the stream has been reached
            if (data.Position >= data.Length)
                return new NewEvent();

            // If the skip amount needs to be determined
            if (op0x18skip == -1)
            {
                long current = data.Position;
                byte nextByte = data.ReadByteValue();
                data.Seek(current, SeekOrigin.Begin);

                op0x18skip = nextByte == 0 || nextByte == 0xFF ? 6 : 0;
                if (nextByte == 0x09)
                {
                    var possible = ParseCallDllFunction(data, languageCount, shortDllCall);
                    op0x18skip = (possible.FunctionName == null || possible.FunctionName.Length == 0) ? 6 : 0;
                    data.Seek(current, SeekOrigin.Begin);
                }
            }

            var obj = new NewEvent();

            // Skip additional bytes
            if (op0x18skip > 0)
                obj.Padding = data.ReadBytes(op0x18skip);

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a Unknown0x19
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled Unknown0x19 on success, null on error</returns>
        private static Unknown0x19 ParseUnknown0x19(Stream data)
        {
            var obj = new Unknown0x19();

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
            var obj = new IncludeScript();

            obj.Count = 0;
            while (data.Position < data.Length && data.ReadByteValue() == 0x1B)
            {
                obj.Count++;
            }

            // Rewind if one was found
            if (data.Position < data.Length)
                data.Seek(-1, SeekOrigin.Current);

            return obj;
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

        /// <summary>
        /// Parse a Stream into a Unknown0x24
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled Unknown0x24 on success, null on error</returns>
        private static Unknown0x24 ParseUnknown0x24(Stream data)
        {
            return new Unknown0x24();
        }

        /// <summary>
        /// Parse a Stream into a Unknown0x25
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled Unknown0x25 on success, null on error</returns>
        private static Unknown0x25 ParseUnknown0x25(Stream data)
        {
            return new Unknown0x25();
        }

        /// <summary>
        /// Parse a Stream into a InvalidOperation
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled InvalidOperation on success, null on error</returns>
        /// <remarks>
        /// This represents a placeholder. The operations that result in this
        /// type being parsed should never happen in the state machine.
        /// </remarks>
        private static InvalidOperation ParseInvalidOperation(Stream data)
        {
            return new InvalidOperation();
        }

        #endregion

        #region Function Actions

        /// <summary>
        /// Parse a string into a AddDirectoryToPath
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled AddDirectoryToPath on success, null on error</returns>
        private static AddDirectoryToPath ParseAddDirectoryToPath(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new AddDirectoryToPath();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Directory = parts[1];

            return obj;
        }

        /// <summary>
        /// Parse a string into a AddToAutoexecBat
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled AddToAutoexecBat on success, null on error</returns>
        private static AddToAutoexecBat ParseAddToAutoexecBat(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new AddToAutoexecBat();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.FileToEdit = parts[1];

            if (parts.Length > 2)
                obj.TextToInsert = parts[2];

            if (parts.Length > 3)
                obj.SearchForText = parts[3];

            if (parts.Length > 4)
                obj.CommentText = parts[4];

            if (parts.Length > 5 && int.TryParse(parts[5], out int lineNumber))
                obj.LineNumber = lineNumber;

            return obj;
        }

        /// <summary>
        /// Parse a string into a AddToConfigSys
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled AddToConfigSys on success, null on error</returns>
        private static AddToConfigSys ParseAddToConfigSys(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new AddToConfigSys();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.FileToEdit = parts[1];

            if (parts.Length > 2)
                obj.TextToInsert = parts[2];

            if (parts.Length > 3)
                obj.SearchForText = parts[3];

            if (parts.Length > 4)
                obj.CommentText = parts[4];

            if (parts.Length > 5 && int.TryParse(parts[5], out int lineNumber))
                obj.LineNumber = lineNumber;

            return obj;
        }

        /// <summary>
        /// Parse a string into a AddToSystemIni
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled AddToSystemIni on success, null on error</returns>
        private static AddToSystemIni ParseAddToSystemIni(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new AddToSystemIni();

            if (parts.Length > 0)
                obj.DeviceName = parts[0];

            return obj;
        }

        /// <summary>
        /// Parse a string into a ReadIniValue
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled ReadIniValue on success, null on error</returns>
        private static ReadIniValue ParseReadIniValue(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new ReadIniValue();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Variable = parts[1];

            if (parts.Length > 2)
                obj.Pathname = parts[2];

            if (parts.Length > 3)
                obj.Section = parts[3];

            if (parts.Length > 4)
                obj.Item = parts[4];

            if (parts.Length > 5)
                obj.DefaultValue = parts[5];

            return obj;
        }

        /// <summary>
        /// Parse a string into a GetRegistryKeyValue
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled GetRegistryKeyValue on success, null on error</returns>
        private static GetRegistryKeyValue ParseGetRegistryKeyValue(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new GetRegistryKeyValue();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Variable = parts[1];

            if (parts.Length > 2)
                obj.Key = parts[2];

            if (parts.Length > 3)
                obj.Default = parts[3];

            if (parts.Length > 4)
                obj.ValueName = parts[4];

            if (parts.Length > 5)
                obj.Root = parts[5];

            return obj;
        }

        /// <summary>
        /// Parse a string into a RegisterFont
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled RegisterFont on success, null on error</returns>
        private static RegisterFont ParseRegisterFont(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new RegisterFont();

            if (parts.Length > 0)
                obj.FontFileName = parts[0];

            if (parts.Length > 1)
                obj.FontName = parts[1];

            return obj;
        }

        /// <summary>
        /// Parse a string into a Win32SystemDirectory
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled Win32SystemDirectory on success, null on error</returns>
        private static Win32SystemDirectory ParseWin32SystemDirectory(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new Win32SystemDirectory();

            if (parts.Length > 0)
                obj.VariableName = parts[0];

            return obj;
        }

        /// <summary>
        /// Parse a string into a CheckConfiguration
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled CheckConfiguration on success, null on error</returns>
        private static CheckConfiguration ParseCheckConfiguration(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new CheckConfiguration();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Message = parts[1];

            if (parts.Length > 2)
                obj.Title = parts[2];

            return obj;
        }

        /// <summary>
        /// Parse a string into a SearchForFile
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled SearchForFile on success, null on error</returns>
        private static SearchForFile ParseSearchForFile(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new SearchForFile();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Variable = parts[1];

            if (parts.Length > 2)
                obj.FileName = parts[2];

            if (parts.Length > 3)
                obj.FileName = parts[3];

            if (parts.Length > 4)
                obj.MessageText = parts[4];

            return obj;
        }

        /// <summary>
        /// Parse a string into a ReadWriteBinaryFile
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled ReadWriteBinaryFile on success, null on error</returns>
        private static ReadWriteBinaryFile ParseReadWriteBinaryFile(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new ReadWriteBinaryFile();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.FilePathname = parts[1];

            if (parts.Length > 2)
                obj.VariableName = parts[2];

            if (parts.Length > 3 && int.TryParse(parts[3], out int fileOffset))
                obj.FileOffset = fileOffset;

            if (parts.Length > 4 && int.TryParse(parts[4], out int maxLength))
                obj.MaxLength = maxLength;

            return obj;
        }

        /// <summary>
        /// Parse a string into a SetVariable
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled SetVariable on success, null on error</returns>
        private static SetVariable ParseSetVariable(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new SetVariable();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Variable = parts[1];

            if (parts.Length > 2)
                obj.Value = parts[2];

            return obj;
        }

        /// <summary>
        /// Parse a string into a GetEnvironmentVariable
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled GetEnvironmentVariable on success, null on error</returns>
        private static GetEnvironmentVariable ParseGetEnvironmentVariable(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new GetEnvironmentVariable();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Variable = parts[1];

            if (parts.Length > 2)
                obj.Environment = parts[2];

            if (parts.Length > 3)
                obj.DefaultValue = parts[3];

            return obj;
        }

        /// <summary>
        /// Parse a string into a CheckIfFileDirExists
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled CheckIfFileDirExists on success, null on error</returns>
        private static CheckIfFileDirExists ParseCheckIfFileDirExists(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new CheckIfFileDirExists();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Pathname = parts[1];

            if (parts.Length > 2)
                obj.Message = parts[2];

            if (parts.Length > 3)
                obj.Title = parts[3];

            return obj;
        }

        /// <summary>
        /// Parse a string into a SetFileAttributes
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled SetFileAttributes on success, null on error</returns>
        private static SetFileAttributes ParseSetFileAttributes(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new SetFileAttributes();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.FilePathname = parts[1];

            return obj;
        }

        /// <summary>
        /// Parse a string into a SetFilesBuffers
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled SetFilesBuffers on success, null on error</returns>
        private static SetFilesBuffers ParseSetFilesBuffers(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new SetFilesBuffers();

            if (parts.Length > 0)
                obj.MinimumFiles = parts[0];

            if (parts.Length > 1)
                obj.MinimumBuffers = parts[1];

            return obj;
        }

        /// <summary>
        /// Parse a string into a FindFileInPath
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled FindFileInPath on success, null on error</returns>
        private static FindFileInPath ParseFindFileInPath(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new FindFileInPath();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.VariableName = parts[1];

            if (parts.Length > 2)
                obj.FileName = parts[2];

            if (parts.Length > 3)
                obj.DefaultValue = parts[3];

            if (parts.Length > 4)
                obj.SearchDirectories = parts[4];

            if (parts.Length > 5)
                obj.Description = parts[5];

            return obj;
        }

        /// <summary>
        /// Parse a string into a CheckDiskSpace
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled CheckDiskSpace on success, null on error</returns>
        private static CheckDiskSpace ParseCheckDiskSpace(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new CheckDiskSpace();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.ReserveSpace = parts[1];

            if (parts.Length > 2)
                obj.StatusVariable = parts[2];

            if (parts.Length > 3)
                obj.ComponentVariables = parts[3];

            return obj;
        }

        /// <summary>
        /// Parse a string into a InsertLineIntoTextFile
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled InsertLineIntoTextFile on success, null on error</returns>
        private static InsertLineIntoTextFile ParseInsertLineIntoTextFile(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new InsertLineIntoTextFile();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.FileToEdit = parts[1];

            if (parts.Length > 2)
                obj.TextToInsert = parts[2];

            if (parts.Length > 3)
                obj.SearchForText = parts[3];

            if (parts.Length > 4)
                obj.CommentText = parts[4];

            if (parts.Length > 5 && int.TryParse(parts[5], out int lineNumber))
                obj.LineNumber = lineNumber;

            return obj;
        }

        /// <summary>
        /// Parse a string into a ParseString
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled ParseString on success, null on error</returns>
        private static ParseString ParseParseString(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new ParseString();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Source = parts[1];

            if (parts.Length > 2)
                obj.PatternPosition = parts[2];

            if (parts.Length > 3)
                obj.DestinationVariable1 = parts[3];

            if (parts.Length > 4)
                obj.DestinationVariable2 = parts[4];

            return obj;
        }

        /// <summary>
        /// Parse a string into a ExitInstallation
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled ExitInstallation on success, null on error</returns>
        private static ExitInstallation ParseExitInstallation(string data)
        {
            return new ExitInstallation();
        }

        /// <summary>
        /// Parse a string into a SelfRegisterOCXsDLLs
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled SelfRegisterOCXsDLLs on success, null on error</returns>
        private static SelfRegisterOCXsDLLs ParseSelfRegisterOCXsDLLs(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new SelfRegisterOCXsDLLs();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Description = parts[1];

            return obj;
        }

        /// <summary>
        /// Parse a string into a InstallDirectXComponents
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled InstallDirectXComponents on success, null on error</returns>
        private static InstallDirectXComponents ParseInstallDirectXComponents(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new InstallDirectXComponents();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.RootPath = parts[1];

            if (parts.Length > 2)
                obj.LibraryPath = parts[2];

            if (parts.Length > 3 && int.TryParse(parts[3], out int sizeOrOffsetOrFlag))
                obj.SizeOrOffsetOrFlag = sizeOrOffsetOrFlag;

            return obj;
        }

        /// <summary>
        /// Parse a string into a WizardBlockLoop
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled WizardBlockLoop on success, null on error</returns>
        private static WizardBlockLoop ParseWizardBlockLoop(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new WizardBlockLoop();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            // TODO: This needs to be fixed when the model is updated

            if (parts.Length > 1)
                obj.DirectionVariable = parts[1];

            if (parts.Length > 2)
                obj.DisplayVariable = parts[2];

            if (parts.Length > 3 && int.TryParse(parts[3], out int xPosition))
                obj.XPosition = xPosition;

            if (parts.Length > 4 && int.TryParse(parts[4], out int yPosition))
                obj.YPosition = yPosition;

            if (parts.Length > 5 && int.TryParse(parts[5], out int fillerColor))
                obj.FillerColor = fillerColor;

            if (parts.Length > 6)
                obj.Operand_6 = parts[6];

            if (parts.Length > 7)
                obj.Operand_7 = parts[7];

            if (parts.Length > 8)
                obj.Operand_8 = parts[8];

            if (parts.Length > 9)
                obj.DialogVariableValueCompare = parts[9];

            return obj;
        }

        /// <summary>
        /// Parse a string into a ReadUpdateTextFile
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled ReadUpdateTextFile on success, null on error</returns>
        private static ReadUpdateTextFile ParseReadUpdateTextFile(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new ReadUpdateTextFile();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.Variable = parts[1];

            if (parts.Length > 2)
                obj.Pathname = parts[2];

            if (parts.Length > 3)
                obj.LanguageStrings = parts[3];

            return obj;
        }

        /// <summary>
        /// Parse a string into a PostToHttpServer
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled PostToHttpServer on success, null on error</returns>
        private static PostToHttpServer ParsePostToHttpServer(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new PostToHttpServer();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.URL = parts[1];

            if (parts.Length > 2)
                obj.PostData = parts[2];

            return obj;
        }

        /// <summary>
        /// Parse a string into a PromptForFilename
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled PromptForFilename on success, null on error</returns>
        private static PromptForFilename ParsePromptForFilename(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new PromptForFilename();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte flags))
                obj.DataFlags = flags;

            if (parts.Length > 1)
                obj.DestinationVariable = parts[1];

            if (parts.Length > 2)
                obj.DefaultExtension = parts[2];

            if (parts.Length > 3)
                obj.DialogTitle = parts[3];

            if (parts.Length > 4)
                obj.FilterList = parts[4];

            return obj;
        }

        /// <summary>
        /// Parse a string into a StartStopService
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled StartStopService on success, null on error</returns>
        private static StartStopService ParseStartStopService(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new StartStopService();

            if (parts.Length > 0 && byte.TryParse(parts[0], out byte operation))
                obj.Operation = operation;

            if (parts.Length > 1)
                obj.ServiceName = parts[1];

            return obj;
        }

        /// <summary>
        /// Parse a string into a CheckHttpConnection
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled CheckHttpConnection on success, null on error</returns>
        private static CheckHttpConnection ParseCheckHttpConnection(string data)
        {
            string[] parts = data.Split((char)0x7F);

            var obj = new CheckHttpConnection();

            if (parts.Length > 0)
                obj.UrlToCheck = parts[0];

            if (parts.Length > 1)
                obj.Win32ErrorTextVariable = parts[1];

            if (parts.Length > 2)
                obj.Win32ErrorNumberVariable = parts[2];

            if (parts.Length > 3)
                obj.Win16ErrorTextVariable = parts[3];

            if (parts.Length > 4)
                obj.Win16ErrorNumberVariable = parts[4];

            return obj;
        }

        /// <summary>
        /// Parse a string into a ExternalDllCall
        /// </summary>
        /// <param name="data">0x7F-separated string to parse</param>
        /// <returns>Filled ExternalDllCall on success, null on error</returns>
        private static ExternalDllCall ParseExternalDllCall(string data)
        {
            var obj = new ExternalDllCall();

            obj.Args = data.Split((char)0x7F);

            return obj;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns if a string may be a typical control code
        /// </summary>
        /// <param name="str">String to check</param>
        /// <param name="strict">Indicates if control codes should always be checked</param>
        /// <returns>True if the string probably represents a control code, false otherwise</returns>
        private static bool IsTypicalControlCode(string str, bool strict)
        {
            // Safeguard against odd cases
            if (str.Length == 0)
                return strict;

            char firstChar = str[0];

            // If there is no worry about newline trickery
            if (strict)
                return firstChar >= (char)0x00 && firstChar <= (char)0x25;

            if (firstChar < (char)0x0A)
                return true;
            else if (firstChar == (char)0x0A && str.Length == 1)
                return true;
            else if (firstChar > (char)0x0A && firstChar < (char)0x0D)
                return true;
            else if (firstChar == (char)0x0D && str.Length == 1)
                return true;
            else if (firstChar > (char)0x0D && firstChar < (char)0x20)
                return true;
            else if (firstChar > (char)0x20 && firstChar <= (char)0x25 && str.Length == 1)
                return true;

            return false;
        }

        /// <summary>
        /// Parse a Stream into a DeflateEntry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled DeflateEntry on success, null on error</returns>
        private static DeflateEntry ParseDeflateEntry(Stream data)
        {
            var obj = new DeflateEntry();

            obj.DeflateStart = data.ReadUInt32LittleEndian();
            obj.DeflateEnd = data.ReadUInt32LittleEndian();
            obj.InflatedSize = data.ReadUInt32LittleEndian();

            return obj;
        }

        #endregion
    }
}
