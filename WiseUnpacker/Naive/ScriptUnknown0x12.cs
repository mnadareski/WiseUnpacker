namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// File on install medium
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptUnknown0x12
    {
        /// <summary>
        /// Unknown, 0x0C
        /// </summary>
        public byte Unknown_1 { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>41 bytes</remarks>
        public byte[]? Unknown_41 { get; set; }

        /// <summary>
        /// Source file 
        /// </summary>
        public string? SourceFile { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public string? UnknownString_1 { get; set; }

        /// <summary>
        /// Unknown string(s), one per language
        /// </summary>
        public string[]? UnknownStrings { get; set; }

        /// <summary>
        /// Destination file
        /// </summary>
        public string? DestFile { get; set; }
    }
}
