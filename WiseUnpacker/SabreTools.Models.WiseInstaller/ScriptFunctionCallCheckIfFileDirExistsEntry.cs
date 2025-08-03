namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Entry data for a Check if File/Dir Exists function call
    /// </summary>
    public class ScriptFunctionCallCheckIfFileDirExistsEntry : ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// Pathname
        /// </summary>
        public string? Pathname { get; set; }
    }
}