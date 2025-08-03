namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Entry data for a Get Environment Variable function call
    /// </summary>
    public class ScriptFunctionCallGetEnvironmentVariableEntry : ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Environment variable name
        /// </summary>
        public string? Environment { get; set; }
    }
}