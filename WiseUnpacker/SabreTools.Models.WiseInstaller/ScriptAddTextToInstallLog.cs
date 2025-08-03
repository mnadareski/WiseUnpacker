namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Add Text to INSTALL.LOG
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptAddTextToInstallLog : MachineStateData
    {
        /// <summary>
        /// Text
        /// </summary>
        public string? Text { get; set; }
    }
}
