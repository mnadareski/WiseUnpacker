namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Display Billboard
    /// 
    /// This action displays a bitmap or .GRF file during installation if you have set the
    /// background to display a gradient on the Screen page. Create .GRF files (scalable
    /// bitmaps) with the Custom Billboard Editor.
    /// 
    /// You can use up to 16 Display Billboard actions in the script.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class DisplayBillboard : MachineStateData
    {
        /// <summary>
        /// Flags
        /// </summary>
        /// <remarks>
        /// Expected values:
        /// - 0x3805 - Erase num?; Erase all?
        /// - 0x4000 - Hide progress bar
        /// </remarks>
        public ushort Flags { get; set; } // 0x01 - 0x02

        /// <summary>
        /// Flags(?)
        /// </summary>
        /// <remarks>
        /// Values from WISE0001.DLL
        /// - >> 0x0F -> piVar3[10]
        /// - & 0x4000 -> uVar8 = Operand_2 & 0x3FFF
        /// - Unknown (0x8000)
        /// </remarks>
        public ushort Operand_2 { get; set; } // 0x03 - 0x04

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>
        /// Values from WISE0001.DLL
        /// - & 0x4000 -> uVar8 = Operand_3 & 0x3FFF
        /// </remarks>
        public ushort Operand_3 { get; set; } // 0x05 - 0x06

        /// <summary>
        /// Deflate information
        /// </summary>
        /// <remarks> One per language</remarks>
        public ScriptDeflateInfo[]? DeflateInfo { get; set; } // 0x07 - 

        /// <summary>
        /// Terminator?
        /// </summary>
        public byte Terminator { get; set; }
    }
}
