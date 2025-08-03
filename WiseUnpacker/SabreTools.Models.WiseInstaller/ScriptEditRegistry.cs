namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Edit registry
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptEditRegistry : MachineStateData
    {
        /// <summary>
        /// Root
        /// </summary>
        public byte Root { get; set; }

        /// <summary>
        /// Data type, defaults to 0 if not defined
        /// in source scripts
        /// </summary>
        public byte DataType { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public byte Operand_3 { get; set; }

        /// <summary>
        /// Key path
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// New value
        /// </summary>
        public string? NewValue { get; set; }

        /// <summary>
        /// Value name
        /// </summary>
        public string? ValueName { get; set; }
    }
}
