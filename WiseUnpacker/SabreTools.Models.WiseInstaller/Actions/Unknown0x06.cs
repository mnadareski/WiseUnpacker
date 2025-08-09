namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Display Billboard
    /// 
    /// 
    /// </summary>
    /// <remarks>
    /// This is likely compressed Billboard data, though it is
    /// very difficult to determine what the format of the extracted
    /// data is. The official documentation mentions that it's
    /// a vector format, but it does not appear to be natively
    /// openable with standard graphics viewing programs.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class Unknown0x06 : MachineStateData
    {
        /// <summary>
        /// Flags(?)
        /// </summary>
        /// <remarks>
        /// Values from WISE0001.DLL
        /// - & 0x4000 != 0 -> FUN_1000bb6a
        /// - & 0x3805 != 0 -> 
        /// 
        /// </remarks>
        public ushort Operand_1 { get; set; } // 0x01 - 0x02

        /// <summary>
        /// Flags(?)
        /// </summary>
        /// <remarks>
        /// Values from WISE0001.DLL
        /// - >> 0x0F -> piVar3[10]
        /// - & 0x4000 -> uVar8 = Operand_2 & 0x3FFF
        /// - [0x04] & 0x80 != 0 -> 
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
        public ScriptDeflateInfoContainer? DeflateInfo { get; set; } // 0x07 - 

        /// <summary>
        /// Terminator?
        /// </summary>
        public byte Terminator { get; set; }
    }
}
