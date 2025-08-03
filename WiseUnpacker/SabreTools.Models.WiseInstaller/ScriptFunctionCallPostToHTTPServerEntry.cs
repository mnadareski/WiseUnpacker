namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Entry data for a Post to HTTP Server function call
    /// </summary>
    public class ScriptFunctionCallPostToHTTPServerEntry : ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// URL
        /// </summary>
        public string? URL { get; set; }
    }
}