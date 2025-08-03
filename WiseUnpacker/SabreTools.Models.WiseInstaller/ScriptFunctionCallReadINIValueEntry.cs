namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Entry data for a Read INI Value function call
    /// </summary>
    public class ScriptFunctionCallReadINIValueEntry : ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Path to INI
        /// </summary>
        public string? Pathname { get; set; }

        /// <summary>
        /// INI section
        /// </summary>
        public string? Section { get; set; }

        /// <summary>
        /// Item key
        /// </summary>
        public string? Item { get; set; }
    }
}