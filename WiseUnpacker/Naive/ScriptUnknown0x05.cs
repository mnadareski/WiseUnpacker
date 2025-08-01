namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Something with the .ini file, write .ini file?
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptUnknown0x05
    {
        /// <summary>
        /// Open for writing in append mode
        /// </summary>
        public string? File { get; set; }

        /// <summary>
        /// INI section text
        /// </summary>
        public string? Section { get; set; }

        /// <summary>
        /// Multiline string containing values
        /// </summary>
        public string? Values { get; set; }
    }
}
