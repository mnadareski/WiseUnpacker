namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// ElseIf Statement
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptElseIf : MachineStateData
    {
        /// <summary>
        /// Operator, values need to be mapped
        /// </summary>
        public byte Operator { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string? Value { get; set; }
    }
}
