namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Display Message
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptDisplayMessage : MachineStateData
    {
        /// <summary>
        /// Flags, unknown mapping
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// Strings, two per language (1 title and 1 message)
        /// </summary>
        public string[]? TitleText { get; set; }
    }
}
