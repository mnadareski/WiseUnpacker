namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Entry data for a Parse String function call
    /// </summary>
    public class ScriptFunctionCallParseStringEntry : ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// Message
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Pattern
        /// </summary>
        public string? Pattern { get; set; }

        /// <summary>
        /// Variable 1
        /// </summary>
        public string? Variable1 { get; set; }
    }
}