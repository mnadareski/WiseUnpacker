using System.Text;
using SabreTools.Models.WiseInstaller;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Printers
{
    public class WiseScript : IPrinter<ScriptFile>
    {
        /// <inheritdoc/>
        public void PrintInformation(StringBuilder builder, ScriptFile model)
            => Print(builder, model);

        public static void Print(StringBuilder builder, ScriptFile scriptFile)
        {
            builder.AppendLine("Wise Installer Script File Information:");
            builder.AppendLine("-------------------------");
            builder.AppendLine();

            Print(builder, scriptFile.Header);
            Print(builder, scriptFile.States);
        }

        private static void Print(StringBuilder builder, ScriptHeader? header)
        {
            builder.AppendLine("  Header Information:");
            builder.AppendLine("  -------------------------");
            if (header == null)
            {
                builder.AppendLine("  No header");
                builder.AppendLine();
                return;
            }

            builder.AppendLine(header.Unknown_5, "  Unknown");
            builder.AppendLine(header.SomeOffset1, "  Unknown");
            builder.AppendLine(header.SomeOffset2, "  Unknown");
            builder.AppendLine(header.Unknown_4, "  Unknown");
            builder.AppendLine(header.DateTime, "  Datetime");
            builder.AppendLine(header.Unknown_22, "  Unknown");
            builder.AppendLine(header.Url, "  URL");
            builder.AppendLine(header.LogPath, "  Log path");
            builder.AppendLine(header.MessageFont, "  Font");
            builder.AppendLine(header.FontSize, "  Font size");
            builder.AppendLine(header.Unknown_2, "  Unknown");
            builder.AppendLine(header.LanguageCount, "  Language count");
            builder.AppendLine();
            builder.AppendLine("  Unknown strings");
            builder.AppendLine("  -------------------------");
            if (header.UnknownStrings_7 == null || header.UnknownStrings_7.Length == 0)
            {
                builder.AppendLine("  No unknown strings");
            }
            else
            {
                for (int i = 0; i < header.UnknownStrings_7.Length; i++)
                {
                    var entry = header.UnknownStrings_7[i];
                    builder.AppendLine($"  Unknown String {i}: {entry}");
                }
            }
            builder.AppendLine();

            builder.AppendLine("  Language selection strings");
            builder.AppendLine("  -------------------------");
            if (header.LanguageSelectionStrings == null || header.LanguageSelectionStrings.Length == 0)
            {
                builder.AppendLine("  No language selection strings");
            }
            else
            {
                for (int i = 0; i < header.LanguageSelectionStrings.Length; i++)
                {
                    var entry = header.LanguageSelectionStrings[i];
                    builder.AppendLine($"  Language Selection String {i}: {entry}");
                }
            }
            builder.AppendLine();

            builder.AppendLine("  Script strings");
            builder.AppendLine("  -------------------------");
            if (header.ScriptStrings == null || header.ScriptStrings.Length == 0)
            {
                builder.AppendLine("  No script strings");
            }
            else
            {
                for (int i = 0; i < header.ScriptStrings.Length; i++)
                {
                    var entry = header.ScriptStrings[i];
                    builder.AppendLine($"  Script String {i}: {entry}");
                }
            }
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, MachineState[]? entries)
        {
            builder.AppendLine("  State Machine Information:");
            builder.AppendLine("  -------------------------");
            if (entries == null || entries.Length == 0)
            {
                builder.AppendLine("  No state machine items");
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                builder.AppendLine($"  State Machine Entry {i}:");
                builder.AppendLine($"    Op: {entry.Op} (0x{(byte)entry.Op:X2})");
                switch (entry.Data)
                {
                    case ScriptFileHeader data: Print(builder, data); break;
                    case ScriptUnknown0x03 data: Print(builder, data); break;
                    case ScriptFormData data: Print(builder, data); break;
                    case ScriptEditIniFile data: Print(builder, data); break;
                    case ScriptUnknown0x06 data: Print(builder, data); break;
                    case ScriptExecuteProgram data: Print(builder, data); break;
                    case ScriptEndBlock data: Print(builder, data); break;
                    case ScriptFunctionCall data: Print(builder, data); break;
                    case ScriptEditRegistry data: Print(builder, data); break;
                    case ScriptDeleteFile data: Print(builder, data); break;
                    case ScriptIfWhileStatement data: Print(builder, data); break;
                    case ScriptUnknown0x11 data: Print(builder, data); break;
                    case ScriptUnknown0x12 data: Print(builder, data); break;
                    case ScriptCustomDialogSet data: Print(builder, data); break;
                    case ScriptGetSystemInformation data: Print(builder, data); break;
                    case ScriptGetTemporaryFilename data: Print(builder, data); break;
                    case ScriptUnknown0x17 data: Print(builder, data); break;
                    case ScriptUnknown0x19 data: Print(builder, data); break;
                    case ScriptUnknown0x1A data: Print(builder, data); break;
                    case ScriptAddTextToInstallLog data: Print(builder, data); break;
                    case ScriptUnknown0x1D data: Print(builder, data); break;
                    case ScriptCompilerVariableIf data: Print(builder, data); break;
                    case ScriptUnknown0x23 data: Print(builder, data); break;
                    case ScriptUnknown0x30 data: Print(builder, data); break;

                    // TODO: Implement printers for all types
                    default: builder.AppendLine("    Data: [NULL]"); break;
                }
            }
        }

        private static void Print(StringBuilder builder, ScriptFileHeader data)
        {
            builder.AppendLine($"    Data: ScriptFileHeader");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine(data.DeflateStart, $"      Deflate start");
            builder.AppendLine(data.DeflateEnd, $"      Deflate end");
            builder.AppendLine(data.Date, $"      Date");
            builder.AppendLine(data.Time, $"      Time");
            builder.AppendLine(data.InflatedSize, $"      Inflated size");
            builder.AppendLine(data.Operand_7, $"      Unknown");
            builder.AppendLine(data.Crc32, $"      CRC-32");
            builder.AppendLine(data.DestFile, $"      Destination file");
            builder.AppendLine($"      File texts");
            builder.AppendLine("      -------------------------");
            if (data.FileTexts == null || data.FileTexts.Length == 0)
            {
                builder.AppendLine("      No file texts");
            }
            else
            {
                for (int i = 0; i < data.FileTexts.Length; i++)
                {
                    var entry = data.FileTexts[i];
                    builder.AppendLine($"      File Text {i}: {entry}");
                }
            }
            builder.AppendLine(data.Operand_11, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x03 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x03");
            builder.AppendLine(data.Operand_0, $"      Unknown");
            builder.AppendLine($"      Language strings");
            builder.AppendLine("      -------------------------");
            if (data.LangStrings == null || data.LangStrings.Length == 0)
            {
                builder.AppendLine("      No language strings");
            }
            else
            {
                for (int i = 0; i < data.LangStrings.Length; i++)
                {
                    var entry = data.LangStrings[i];
                    builder.AppendLine($"      Language String {i}: {entry}");
                }
            }
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptFormData data)
        {
            builder.AppendLine($"    Data: ScriptFormData");
            builder.AppendLine(data.No, $"      No");
            builder.AppendLine($"      Language strings");
            builder.AppendLine("      -------------------------");
            if (data.LangStrings == null || data.LangStrings.Length == 0)
            {
                builder.AppendLine("      No language strings");
            }
            else
            {
                for (int i = 0; i < data.LangStrings.Length; i++)
                {
                    var entry = data.LangStrings[i];
                    builder.AppendLine($"      Language String {i}: {entry}");
                }
            }
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptEditIniFile data)
        {
            builder.AppendLine($"    Data: ScriptEditIniFile");
            builder.AppendLine(data.Pathname, $"      Pathname");
            builder.AppendLine(data.Section, $"      Section");
            builder.AppendLine(data.Values, $"      Values");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x06 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x06");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            Print(builder, data.DeflateInfo, 6);
            builder.AppendLine(data.Terminator, $"      Terminator");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptDeflateInfoContainer? data, int indent)
        {
            string padding = string.Empty.PadLeft(indent, ' ');

            builder.AppendLine($"{padding}Deflate info container:");
            builder.AppendLine($"{padding}-------------------------");
            builder.AppendLine($"{padding}Info:");
            if (data?.Info == null || data.Info.Length == 0)
            {
                builder.AppendLine("  No deflate info items");
                return;
            }

            for (int i = 0; i < data.Info.Length; i++)
            {
                var entry = data.Info[i];
                Print(builder, entry, indent + 2, i);
            }
        }

        private static void Print(StringBuilder builder, ScriptDeflateInfo data, int indent, int index = -1)
        {
            string padding = string.Empty.PadLeft(indent, ' ');

            if (index >= 0)
                builder.AppendLine($"{padding}Deflate info {index}");
            else
                builder.AppendLine($"{padding}Deflate info");

            builder.AppendLine($"{padding}-------------------------");
            builder.AppendLine(data.DeflateStart, $"{padding}  Deflate start");
            builder.AppendLine(data.DeflateEnd, $"{padding}  Deflate end");
            builder.AppendLine(data.InflatedSize, $"{padding}  Inflated size");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptExecuteProgram data)
        {
            builder.AppendLine($"    Data: ScriptExecuteProgram");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.Pathname, $"      Pathname");
            builder.AppendLine(data.CommandLine, $"      Command Line");
            builder.AppendLine(data.DefaultDirectory, $"      Default Directory");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptEndBlock data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x08");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptFunctionCall data)
        {
            builder.AppendLine($"    Data: ScriptFunctionCall");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine(data.DllPath, $"      DLL path");
            builder.AppendLine(data.FunctionName, $"      Function name");
            builder.AppendLine(data.Operand_4, $"      Unknown");
            builder.AppendLine(data.ReturnVariable, $"      Return variable");
            builder.AppendLine($"      Entries");
            builder.AppendLine("      -------------------------");
            if (data.Entries == null || data.Entries.Length == 0)
            {
                builder.AppendLine("      No entry strings");
            }
            else
            {
                for (int i = 0; i < data.Entries.Length; i++)
                {
                    var entry = data.Entries[i];
                    builder.AppendLine($"      Entry {i}: {entry}");
                }
            }
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptEditRegistry data)
        {
            builder.AppendLine($"    Data: ScriptEditRegistry");
            builder.AppendLine(data.Root, $"      Root");
            builder.AppendLine(data.Key, $"      Key");
            builder.AppendLine(data.NewValue, $"      New value");
            builder.AppendLine(data.ValueName, $"      Value name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptDeleteFile data)
        {
            builder.AppendLine($"    Data: ScriptDeleteFile");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.Pathname, $"      Pathname");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptIfWhileStatement data)
        {
            builder.AppendLine($"    Data: ScriptIfWhileStatement");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.Variable, $"      Variable");
            builder.AppendLine(data.Value, $"      Value");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x11 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x11");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x12 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x12");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine(data.Operand_2, $"      Unknown");
            builder.AppendLine(data.SourceFile, $"      Source file");
            builder.AppendLine(data.Operand_4, $"      Unknown");
            builder.AppendLine($"      Unknown");
            builder.AppendLine("      -------------------------");
            if (data.Operand_5 == null || data.Operand_5.Length == 0)
            {
                builder.AppendLine("      No unknown strings");
            }
            else
            {
                for (int i = 0; i < data.Operand_5.Length; i++)
                {
                    var entry = data.Operand_5[i];
                    builder.AppendLine($"      Unknown String {i}: {entry}");
                }
            }
            builder.AppendLine();
            builder.AppendLine(data.DestFile, $"      Destination file");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptCustomDialogSet data)
        {
            builder.AppendLine($"    Data: ScriptCustomDialogSet");
            builder.AppendLine(data.DeflateStart, $"      Deflate start");
            builder.AppendLine(data.DeflateEnd, $"      Deflate end");
            builder.AppendLine(data.InflatedSize, $"      Inflated size");
            builder.AppendLine(data.DisplayVariable, $"      Display variable");
            builder.AppendLine(data.Name, $"      Name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptGetSystemInformation data)
        {
            builder.AppendLine($"    Data: ScriptGetSystemInformation");
            builder.AppendLine(data.Variable, $"      Variable");
            builder.AppendLine(data.Operand_3, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptGetTemporaryFilename data)
        {
            builder.AppendLine($"    Data: ScriptGetTemporaryFilename");
            builder.AppendLine(data.Variable, $"      Variable");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x17 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x17");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine(data.Operand_2, $"      Unknown");
            builder.AppendLine(data.Operand_3, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x19 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x19");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine(data.Operand_2, $"      Unknown");
            builder.AppendLine(data.Operand_3, $"      Unknown");
            builder.AppendLine(data.Operand_4, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x1A data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x1A");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine(data.Operand_2, $"      Unknown");
            builder.AppendLine(data.Operand_3, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptAddTextToInstallLog data)
        {
            builder.AppendLine($"    Data: ScriptAddTextToInstallLog");
            builder.AppendLine(data.Text, $"      Text");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x1D data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x1D");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine(data.Operand_2, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptCompilerVariableIf data)
        {
            builder.AppendLine($"    Data: ScriptCompilerVariableIf");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.Variable, $"      Variable");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x23 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x23");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine(data.VarName, $"      Variable name");
            builder.AppendLine(data.VarValue, $"      Variable value");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x30 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x30");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine(data.Operand_2, $"      Variable name");
            builder.AppendLine(data.Operand_3, $"      Variable value");
            builder.AppendLine();
        }
    }
}