namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Copy Local File
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptCopyLocalFile : MachineStateData
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
        public string? Source { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public string? Operand_4 { get; set; }

        /// <summary>
        /// Description, one per language
        /// </summary>
        public string[]? Description { get; set; }

        /// <summary>
        /// Destination file
        /// </summary>
        public string? Destination { get; set; }
    }
}
