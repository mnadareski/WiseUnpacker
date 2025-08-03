namespace SabreTools.Models.WiseInstaller
{
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptUnknown0x17 : MachineStateData
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public byte Operand_1 { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>4 bytes</remarks>
        public byte[]? Operand_2 { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public string? Operand_3 { get; set; }
    }
}
