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
            builder.AppendLine(header.Font, "  Font");
            builder.AppendLine(header.Unknown_6, "  Unknown");
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
                builder.AppendLine($"    Op: {entry.Op} (0x{entry.Op:X1})");
                switch (entry.Data)
                {
                    case ScriptFileHeader data: Print(builder, data); break;
                    case ScriptUnknown0x03 data: Print(builder, data); break;
                    case ScriptUnknown0x04 data: Print(builder, data); break;
                    case ScriptUnknown0x05 data: Print(builder, data); break;
                    case ScriptUnknown0x06 data: Print(builder, data); break;
                    case ScriptUnknown0x07 data: Print(builder, data); break;
                    case ScriptUnknown0x08 data: Print(builder, data); break;
                    case ScriptUnknown0x09 data: Print(builder, data); break;
                    case ScriptUnknown0x0A data: Print(builder, data); break;
                    case ScriptUnknown0x0B data: Print(builder, data); break;
                    case ScriptUnknown0x0C data: Print(builder, data); break;
                    case ScriptUnknown0x11 data: Print(builder, data); break;
                    case ScriptUnknown0x12 data: Print(builder, data); break;
                    case ScriptUnknown0x14 data: Print(builder, data); break;
                    case ScriptUnknown0x15 data: Print(builder, data); break;
                    case ScriptUnknown0x16 data: Print(builder, data); break;
                    case ScriptUnknown0x17 data: Print(builder, data); break;
                    case ScriptUnknown0x19 data: Print(builder, data); break;
                    case ScriptUnknown0x1A data: Print(builder, data); break;
                    case ScriptUnknown0x1C data: Print(builder, data); break;
                    case ScriptUnknown0x1D data: Print(builder, data); break;
                    case ScriptUnknown0x1E data: Print(builder, data); break;
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
            builder.AppendLine(data.Unknown_2, $"      Unknown");
            builder.AppendLine(data.DeflateStart, $"      Deflate start");
            builder.AppendLine(data.DeflateEnd, $"      Deflate end");
            builder.AppendLine(data.Date, $"      Date");
            builder.AppendLine(data.Time, $"      Time");
            builder.AppendLine(data.InflatedSize, $"      Inflated size");
            builder.AppendLine(data.Unknown_20, $"      Unknown");
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
            builder.AppendLine(data.UnknownString, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x03 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x03");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
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

        private static void Print(StringBuilder builder, ScriptUnknown0x04 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x04");
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

        private static void Print(StringBuilder builder, ScriptUnknown0x05 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x05");
            builder.AppendLine(data.File, $"      File");
            builder.AppendLine(data.Section, $"      Section");
            builder.AppendLine(data.Values, $"      Values");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x06 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x06");
            builder.AppendLine(data.Unknown_2, $"      Unknown");
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

        private static void Print(StringBuilder builder, ScriptUnknown0x07 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x07");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_2, $"      Unknown");
            builder.AppendLine(data.UnknownString_3, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x08 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x08");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x09 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x09");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_2, $"      Unknown");
            builder.AppendLine(data.UnknownString_3, $"      Unknown");
            builder.AppendLine(data.UnknownString_4, $"      Unknown");
            builder.AppendLine($"      Unknown");
            builder.AppendLine("      -------------------------");
            if (data.UnknownStrings == null || data.UnknownStrings.Length == 0)
            {
                builder.AppendLine("      No unknown strings");
            }
            else
            {
                for (int i = 0; i < data.UnknownStrings.Length; i++)
                {
                    var entry = data.UnknownStrings[i];
                    builder.AppendLine($"      Unknown String {i}: {entry}");
                }
            }
            builder.AppendLine();
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x0A data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x0A");
            builder.AppendLine(data.Unknown_2, $"      Unknown");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_2, $"      Unknown");
            builder.AppendLine(data.UnknownString_3, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x0B data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x0B");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x0C data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x0C");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine(data.VarName, $"      Variable name");
            builder.AppendLine(data.VarValue, $"      Variable value");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x11 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x11");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x12 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x12");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine(data.Unknown_41, $"      Unknown");
            builder.AppendLine(data.SourceFile, $"      Source file");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine($"      Unknown");
            builder.AppendLine("      -------------------------");
            if (data.UnknownStrings == null || data.UnknownStrings.Length == 0)
            {
                builder.AppendLine("      No unknown strings");
            }
            else
            {
                for (int i = 0; i < data.UnknownStrings.Length; i++)
                {
                    var entry = data.UnknownStrings[i];
                    builder.AppendLine($"      Unknown String {i}: {entry}");
                }
            }
            builder.AppendLine();
            builder.AppendLine(data.DestFile, $"      Destination file");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x14 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x14");
            builder.AppendLine(data.DeflateStart, $"      Deflate start");
            builder.AppendLine(data.DeflateEnd, $"      Deflate end");
            builder.AppendLine(data.InflatedSize, $"      Inflated size");
            builder.AppendLine(data.Name, $"      Name");
            builder.AppendLine(data.Message, $"      Message");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x15 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x15");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_2, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x16 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x16");
            builder.AppendLine(data.Name, $"      Name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x17 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x17");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine(data.Unknown_4, $"      Unknown");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x19 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x19");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_2, $"      Unknown");
            builder.AppendLine(data.UnknownString_3, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x1A data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x1A");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_2, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x1C data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x1C");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x1D data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x1D");
            builder.AppendLine(data.UnknownString_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_2, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x1E data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x1E");
            builder.AppendLine(data.Unknown, $"      Unknown");
            builder.AppendLine(data.UnknownString, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x23 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x23");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine(data.VarName, $"      Variable name");
            builder.AppendLine(data.VarValue, $"      Variable value");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ScriptUnknown0x30 data)
        {
            builder.AppendLine($"    Data: ScriptUnknown0x30");
            builder.AppendLine(data.Unknown_1, $"      Unknown");
            builder.AppendLine(data.UnknownString_1, $"      Variable name");
            builder.AppendLine(data.UnknownString_2, $"      Variable value");
            builder.AppendLine();
        }
    }
}