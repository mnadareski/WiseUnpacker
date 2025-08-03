namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Entry data for a Set Variable function call
    /// </summary>
    public class ScriptFunctionCallSetVariableEntry : ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Value, optional
        /// </summary>
        public string? Value { get; set; }
    }
}