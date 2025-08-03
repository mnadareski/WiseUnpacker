namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Entry data for a Get Registry Key Value function call
    /// </summary>
    public class ScriptFunctionCallGetRegistryKeyValueEntry : ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Registry key
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Default value if not found
        /// </summary>
        public string? Default { get; set; }

        /// <summary>
        /// Value name
        /// </summary>
        public string? ValueName { get; set; }
    }
}