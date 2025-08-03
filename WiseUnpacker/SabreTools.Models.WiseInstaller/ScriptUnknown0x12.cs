namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// File on install medium
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptUnknown0x12 : MachineStateData
    {
        /// <summary>
        /// Unknown, 0x0C
        /// </summary>
        public byte Operand_1 { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>41 bytes</remarks>
        public byte[]? Operand_2 { get; set; }

        /// <summary>
        /// Source file 
        /// </summary>
        public string? SourceFile { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public string? Operand_4 { get; set; }

        /// <summary>
        /// Unknown string(s), one per language
        /// </summary>
        public string[]? Operand_5 { get; set; }

        /// <summary>
        /// Destination file
        /// </summary>
        public string? DestFile { get; set; }
    }
}
