namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Form data?
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptUnknown0x04
    {
        /// <summary>
        /// Read this struct again until 'no' == 0 ?
        /// </summary>
        public byte No { get; set; }

        /// <summary>
        /// One string per language
        /// </summary>
        public string[]? LangStrings { get; set; }
    }
}
