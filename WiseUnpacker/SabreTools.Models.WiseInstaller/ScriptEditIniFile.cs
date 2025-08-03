namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Something with the .ini file, write .ini file?
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptEditIniFile : MachineStateData
    {
        /// <summary>
        /// Path name to INI file
        /// </summary>
        /// <remarks>Open for writing in append mode</remarks>
        public string? Pathname { get; set; }

        /// <summary>
        /// INI section, represented by a Settings line
        /// in the original script
        /// </summary>
        public string? Section { get; set; }

        /// <summary>
        /// Multiline string containing values, each representing
        /// a new Settings line in the original script
        /// </summary>
        public string? Values { get; set; }
    }
}
