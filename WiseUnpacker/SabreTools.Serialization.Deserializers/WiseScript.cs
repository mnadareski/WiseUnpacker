using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.IO.Extensions;
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
            header.Font = data.ReadNullTerminatedAnsiString();

            // If the font string is empty, then the header is trimmed
            if (header.Font != null && header.Font.Length == 0)
            {
                // Seek back to the original position
                data.Seek(current, SeekOrigin.Begin);

                // Recreate the header with minimal data
                header = new ScriptHeader();

                // TODO: Figure out if this maps to existing fields
                header.Unknown_22 = data.ReadBytes(20);
                header.Font = data.ReadNullTerminatedAnsiString();
            }

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
                    OperationCode.CustomDeflateFileHeader => ParseScriptFileHeader(data, languageCount),
                    OperationCode.Unknown0x03 => ParseUnknown0x03(data, languageCount),
                    OperationCode.FormData => ParseScriptFormData(data, languageCount),
                    OperationCode.IniFile => ParseIniFileWrite(data),
                    OperationCode.UnknownDeflatedFile0x06 => ParseUnknown0x06(data, languageCount),
                    OperationCode.Unknown0x07 => ParseUnknown0x07(data),
                    OperationCode.EndBranch => ParseUnknown0x08(data),
                    OperationCode.FunctionCall => ParseUnknown0x09(data, languageCount),
                    OperationCode.Unknown0x0A => ParseUnknown0x0A(data),
                    OperationCode.Unknown0x0B => ParseUnknown0x0B(data),
                    OperationCode.IfStatement => ParseUnknown0x0C(data),
                    OperationCode.ElseStatement => null, // No-op
                    OperationCode.StartFormData => null, // No-op
                    OperationCode.EndFormData => null, // No-op
                    OperationCode.Unknown0x11 => ParseUnknown0x11(data),
                    OperationCode.FileOnInstallMedium => ParseUnknown0x12(data, languageCount),
                    OperationCode.UnknownDeflatedFile0x14 => ParseUnknown0x14(data),
                    OperationCode.Unknown0x15 => ParseUnknown0x15(data),
                    OperationCode.TempFilename => ParseUnknown0x16(data),
                    OperationCode.Unknown0x17 => ParseUnknown0x17(data),
                    OperationCode.Skip0x18 => null, // No-op, handled below
                    OperationCode.Unknown0x19 => ParseUnknown0x19(data),
                    OperationCode.Unknown0x1A => ParseUnknown0x1A(data),
                    OperationCode.Skip0x1B => null, // No-op
                    OperationCode.Unknown0x1C => ParseUnknown0x1C(data),
                    OperationCode.Unknown0x1D => ParseUnknown0x1D(data),
                    OperationCode.Unknown0x1E => ParseUnknown0x1E(data),
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
        /// Parse a Stream into a ScriptIniFileWrite
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptIniFileWrite on success, null on error</returns>
        private static ScriptIniFileWrite ParseIniFileWrite(Stream data)
        {
            var obj = new ScriptIniFileWrite();

            obj.File = data.ReadNullTerminatedAnsiString();
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

            obj.Unknown_2 = data.ReadBytes(2);
            obj.Unknown = data.ReadUInt32LittleEndian();
            obj.DeflateInfo = ParseScriptDeflateInfoContainer(data, languageCount);
            obj.Terminator = data.ReadByteValue();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x07
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x07 on success, null on error</returns>
        private static ScriptUnknown0x07 ParseUnknown0x07(Stream data)
        {
            var obj = new ScriptUnknown0x07();

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_3 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x08
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x08 on success, null on error</returns>
        private static ScriptUnknown0x08 ParseUnknown0x08(Stream data)
        {
            var obj = new ScriptUnknown0x08();

            obj.Unknown_1 = data.ReadByteValue();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x09
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x09 on success, null on error</returns>
        private static ScriptUnknown0x09 ParseUnknown0x09(Stream data, int languageCount)
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
        /// Parse a Stream into a ScriptUnknown0x0A
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x0A on success, null on error</returns>
        private static ScriptUnknown0x0A ParseUnknown0x0A(Stream data)
        {
            var obj = new ScriptUnknown0x0A();

            obj.Unknown_2 = data.ReadUInt16LittleEndian();

            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            if (obj.UnknownString_1 == string.Empty)
                obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();

            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_3 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x0B
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x0B on success, null on error</returns>
        private static ScriptUnknown0x0B ParseUnknown0x0B(Stream data)
        {
            var obj = new ScriptUnknown0x0B();

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x0C
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x0C on success, null on error</returns>
        private static ScriptUnknown0x0C ParseUnknown0x0C(Stream data)
        {
            var obj = new ScriptUnknown0x0C();

            obj.Unknown_1 = data.ReadByteValue();
            obj.VarName = data.ReadNullTerminatedAnsiString();
            obj.VarValue = data.ReadNullTerminatedAnsiString();

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

            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();

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
        /// Parse a Stream into a ScriptUnknown0x14
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x14 on success, null on error</returns>
        private static ScriptUnknown0x14 ParseUnknown0x14(Stream data)
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
        /// Parse a Stream into a ScriptUnknown0x15
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x15 on success, null on error</returns>
        private static ScriptUnknown0x15 ParseUnknown0x15(Stream data)
        {
            var obj = new ScriptUnknown0x15();

            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x16
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x16 on success, null on error</returns>
        private static ScriptUnknown0x16 ParseUnknown0x16(Stream data)
        {
            var obj = new ScriptUnknown0x16();

            obj.Name = data.ReadNullTerminatedAnsiString();

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

            obj.Unknown_1 = data.ReadByteValue();
            obj.Unknown_4 = data.ReadBytes(4);
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();

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

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_3 = data.ReadNullTerminatedAnsiString();

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

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x1C
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x1C on success, null on error</returns>
        private static ScriptUnknown0x1C ParseUnknown0x1C(Stream data)
        {
            var obj = new ScriptUnknown0x1C();

            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();

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

            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ScriptUnknown0x1E
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ScriptUnknown0x1E on success, null on error</returns>
        private static ScriptUnknown0x1E ParseUnknown0x1E(Stream data)
        {
            var obj = new ScriptUnknown0x1E();

            obj.Unknown = data.ReadByteValue();
            obj.UnknownString = data.ReadNullTerminatedAnsiString();

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

            obj.Unknown_1 = data.ReadByteValue();
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

            obj.Unknown_1 = data.ReadByteValue();
            obj.UnknownString_1 = data.ReadNullTerminatedAnsiString();
            obj.UnknownString_2 = data.ReadNullTerminatedAnsiString();

            return obj;
        }
    }
}
