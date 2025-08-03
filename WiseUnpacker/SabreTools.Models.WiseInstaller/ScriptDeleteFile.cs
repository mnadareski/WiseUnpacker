namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Delete File
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptDeleteFile : MachineStateData
    {
        /// <summary>
        /// Flags, unknown values
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// Path name
        /// </summary>
        public string? Pathname { get; set; }
    }
}
