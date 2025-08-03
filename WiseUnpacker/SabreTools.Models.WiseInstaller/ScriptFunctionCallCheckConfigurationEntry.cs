namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Entry data for a Check Configuration function call
    /// </summary>
    public class ScriptFunctionCallCheckConfigurationEntry : ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// Message, optional
        /// </summary>
        /// <remarks>If fully numeric, it was probably called Flags2 in the source</remarks>
        public string? Message { get; set; }

        /// <summary>
        /// Title, optional
        /// </summary>
        public string? Title { get; set; }
    }
}