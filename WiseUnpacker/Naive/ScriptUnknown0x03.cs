namespace SabreTools.Models.WiseInstaller
{
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptUnknown0x03
    {
        /// <summary>
        /// Unknown, maybe flags?
        /// </summary>
        public byte Unknown_1 { get; set; }

        /// <summary>
        /// Error strings, two per language (1 title and 1 message)
        /// </summary>
        public string[]? LangStrings { get; set; }
    }
}
