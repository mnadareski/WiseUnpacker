using System.Text;
using SabreTools.Models.WiseInstaller;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Printers
{
    public class WiseOverlayHeader : IPrinter<OverlayHeader>
    {
        /// <inheritdoc/>
        public void PrintInformation(StringBuilder builder, OverlayHeader model)
            => Print(builder, model);

        public static void Print(StringBuilder builder, OverlayHeader overlayHeader)
        {
#if NET20 || NET35
            bool pkzip = (overlayHeader.Flags & OverlayHeaderFlags.WISE_FLAG_PK_ZIP) != 0;
#else
            bool pkzip = overlayHeader.Flags.HasFlag(OverlayHeaderFlags.WISE_FLAG_PK_ZIP);
#endif

            builder.AppendLine("Wise Installer Overlay Header Information:");
            builder.AppendLine("-------------------------");
            builder.AppendLine(overlayHeader.DllNameLen, "DLL name length");
            builder.AppendLine(overlayHeader.DllName, "DLL name");
            builder.AppendLine(overlayHeader.DllSize, "DLL size");
            builder.AppendLine($"Flags: {overlayHeader.Flags} (0x{(uint)overlayHeader.Flags:X4})");
            builder.AppendLine(pkzip, "  Uses PKZIP containers");
            builder.AppendLine(overlayHeader.GraphicsData, "Graphics data");
            builder.AppendLine(overlayHeader.WiseScriptExitEventOffset, "Wise script exit event offset");
            builder.AppendLine(overlayHeader.WiseScriptCancelEventOffset, "Wise script cancel event offset");
            builder.AppendLine(overlayHeader.WiseScriptInflatedSize, "Wise script inflated size");
            builder.AppendLine(overlayHeader.WiseScriptDeflatedSize, "Wise script deflated size");
            builder.AppendLine(overlayHeader.WiseDllDeflatedSize, "Wise DLL deflated size");
            builder.AppendLine(overlayHeader.Ctl3d32DeflatedSize, "CTL3D32.DLL deflated size");
            builder.AppendLine(overlayHeader.SomeData4DeflatedSize, "FILE0004 deflated size");
            builder.AppendLine(overlayHeader.RegToolDeflatedSize, "Ocxreg32.EXE deflated size");
            builder.AppendLine(overlayHeader.ProgressDllDeflatedSize, "PROGRESS.DLL deflated size");
            builder.AppendLine(overlayHeader.SomeData7DeflatedSize, "FILE0007 deflated size");
            builder.AppendLine(overlayHeader.SomeData8DeflatedSize, "FILE0008 deflated size");
            builder.AppendLine(overlayHeader.SomeData9DeflatedSize, "FILE0009 deflated size");
            builder.AppendLine(overlayHeader.SomeData10DeflatedSize, "FILE000A deflated size");
            builder.AppendLine(overlayHeader.FinalFileDeflatedSize, "FILE000{n}.DAT deflated size");
            builder.AppendLine(overlayHeader.FinalFileInflatedSize, "FILE000{n}.DAT inflated size");
            builder.AppendLine(overlayHeader.EOF, "EOF");
            builder.AppendLine(overlayHeader.DibDeflatedSize, "DIB deflated size");
            builder.AppendLine(overlayHeader.DibInflatedSize, "DIB inflated size");
            builder.AppendLine(overlayHeader.InstallScriptDeflatedSize, "Install script deflated size");
            if (overlayHeader.CharacterSet != null)
                builder.AppendLine($"Character set: {overlayHeader.CharacterSet} (0x{(uint)overlayHeader.CharacterSet:X4})");
            else
                builder.AppendLine((uint?)null, $"Character set");
            builder.AppendLine($"Endianness: {overlayHeader.Endianness} (0x{(uint)overlayHeader.Endianness:X4})");
            builder.AppendLine(overlayHeader.InitTextLen, "Init text length");
            builder.AppendLine(overlayHeader.InitText, "Init text");
            builder.AppendLine();
        }
    }
}