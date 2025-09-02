using System.Text;
using SabreTools.Models.WiseInstaller;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Printers
{
    public class WiseSectionHeader : IPrinter<SectionHeader>
    {
        /// <inheritdoc/>
        public void PrintInformation(StringBuilder builder, SectionHeader model)
            => Print(builder, model);

        public static void Print(StringBuilder builder, SectionHeader header)
        {
            builder.AppendLine("Wise Section Header Information:");
            builder.AppendLine("-------------------------");
            builder.AppendLine(header.UnknownDataSize, "Unknown data size");
            builder.AppendLine(header.SecondExecutableFileEntryLength, "Second executable file entry length");
            builder.AppendLine(header.UnknownValue2, "Unknown value 2");
            builder.AppendLine(header.UnknownValue3, "Unknown value 3");
            builder.AppendLine(header.UnknownValue4, "Unknown value 4");
            builder.AppendLine(header.FirstExecutableFileEntryLength, "First executable file entry length");
            builder.AppendLine(header.MsiFileEntryLength, "MSI file entry length");
            builder.AppendLine(header.UnknownValue7, "Unknown value 7");
            builder.AppendLine(header.UnknownValue8, "Unknown value 8");
            builder.AppendLine(header.ThirdExecutableFileEntryLength, "Third executable file entry length");
            builder.AppendLine(header.UnknownValue10, "Unknown value 10");
            builder.AppendLine(header.UnknownValue11, "Unknown value 11");
            builder.AppendLine(header.UnknownValue12, "Unknown value 12");
            builder.AppendLine(header.UnknownValue13, "Unknown value 13");
            builder.AppendLine(header.UnknownValue14, "Unknown value 14");
            builder.AppendLine(header.UnknownValue15, "Unknown value 15");
            builder.AppendLine(header.UnknownValue16, "Unknown value 16");
            builder.AppendLine(header.UnknownValue17, "Unknown value 17");
            builder.AppendLine(header.UnknownValue18, "Unknown value 18");
            builder.AppendLine(header.Version, "Version");
            builder.AppendLine(header.TmpString, "TMP string");
            builder.AppendLine(header.GuidString, "GUID string");
            builder.AppendLine(header.NonWiseVersion, "Non-Wise version");
            builder.AppendLine(header.PreFontValue, "Pre-font value");
            builder.AppendLine(header.FontSize, "Font size");
            builder.AppendLine(header.PreStringValues, "Pre-string values");
            builder.AppendLine();
            builder.AppendLine("Strings:");
            if (header.Strings == null || header.Strings.Length == 0)
            {
                builder.AppendLine("  No strings");
            }
            else
            {
                for (int i = 0; i < header.Strings.Length; i++)
                {
                    var entry = header.Strings[i];
                    builder.AppendLine($"  String {i}: {entry}");
                }
            }
            builder.AppendLine();
        }
    }
}