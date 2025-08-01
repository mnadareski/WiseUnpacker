namespace SabreTools.Models.WiseInstaller
{
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptUnknown0x09 : MachineStateData
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public byte Unknown_1 { get; set; }

        /// <summary>
        /// DLL path/name or NULL for Wise internal
        /// </summary>
        public string? UnknownString_1 { get; set; }

        /// <summary>
        /// Func name
        /// </summary>
        public string? UnknownString_2 { get; set; }

        /// <summary>
        /// Args?
        /// </summary>
        public string? UnknownString_3 { get; set; }

        /// <summary>
        /// Args?
        /// </summary>
        public string? UnknownString_4 { get; set; }

        /// <summary>
        /// One string per language count
        /// </summary>
        public string[]? UnknownStrings { get; set; }
    }
}
