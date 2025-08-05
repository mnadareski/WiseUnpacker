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
            builder.AppendLine("Wise Installer Overlay Header Information:");
            builder.AppendLine("-------------------------");
            builder.AppendLine(overlayHeader.DllNameLen, "DLL name length");
            builder.AppendLine(overlayHeader.DllName, "DLL name");
            builder.AppendLine(overlayHeader.DllSize, "DLL size");
            builder.AppendLine($"Flags: {overlayHeader.Flags} (0x{(uint)overlayHeader.Flags:X4})");
            builder.AppendLine(overlayHeader.UnknownBytes_1, "Unknown");
            builder.AppendLine(overlayHeader.StartGradient, "Start gradient");
            builder.AppendLine(overlayHeader.EndGradient, "End gradient");
            builder.AppendLine(overlayHeader.UnknownBytes_2, "Unknown");
            builder.AppendLine(overlayHeader.WiseScriptNewEventOffset_1, "Wise script new event offset 1");
            builder.AppendLine(overlayHeader.WiseScriptNewEventOffset_2, "Wise script new event offset 2");
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
            builder.AppendLine(overlayHeader.UnknownU32_4, "Unknown");
            builder.AppendLine($"Endianness: {overlayHeader.Endianness} (0x{(uint)overlayHeader.Endianness:X4})");
            builder.AppendLine(overlayHeader.InitTextLen, "Init text length");
            builder.AppendLine(overlayHeader.InitText, "Init text");
            builder.AppendLine();
        }
    }
}