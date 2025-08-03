namespace SabreTools.Models.WiseInstaller
{
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
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
