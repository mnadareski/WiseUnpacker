namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Add to CONFIG.SYS
    /// 
    /// This action edits the Config.sys file to add new commands. Insert commands at a
    /// particular line number, or search the file for specific text and insert the new line before,
    /// after, or in place of the existing line. The destination computer is restarted automatically
    /// to force the new commands to take effect.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f2".
    /// 
    /// Probably this layout: 
    /// - Flags (numeric) (e.g. "12", "8")
    /// - Unknown string (empty in samples)
    /// - Executable path (e.g. "%WIN%\hcwSubID.exe", "765.exe")
    /// - Executable path again (e.g. "%WIN%\hcwSubID.exe", "765.exe")
    /// - Unknown string (empty in samples)
    /// - Numeric value (e.g. "0")
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class AddToConfigSys : FunctionData
    {
        /// <summary>
        /// Arguments passed in from the Call DLL Function
        /// </summary>
        public string[]? Args { get; set; }
    }
}