namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// TempFileName
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptGetTemporaryFilename : MachineStateData
    {
        /// <summary>
        /// Name, labeled as "Variable" in scripts
        /// </summary>
        public string? Variable { get; set; }
    }
}
