namespace SabreTools.Models.WiseInstaller
{
    /// <remarks>
    /// This is likely compressed Billboard data, though it is
    /// very difficult to determine what the format of the extracted
    /// data is. The official documentation mentions that it's
    /// a vector format, but it does not appear to be natively
    /// openable with standard graphics viewing programs.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ScriptUnknown0x06 : MachineStateData
    {
        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>2 bytes</remarks>
        public byte[]? Operand_1 { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public uint Operand_2 { get; set; }

        /// <summary>
        /// Deflate information
        /// </summary>
        public ScriptDeflateInfoContainer? DeflateInfo { get; set; }

        /// <summary>
        /// Terminator?
        /// </summary>
        public byte Terminator { get; set; }
    }
}
