namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Edit INI File
    /// 
    /// This action edits an .INI file on the destination computer. To edit SYSTEM.INI, use the
    /// Add to SYSTEM.INI action instead.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class EditIniFile : MachineStateData
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
