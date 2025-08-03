namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Get system information variable
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptGetSystemInformation : MachineStateData
    {
        /// <summary>
        /// Flags, values unknown
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public string? Operand_3 { get; set; }
    }
}
