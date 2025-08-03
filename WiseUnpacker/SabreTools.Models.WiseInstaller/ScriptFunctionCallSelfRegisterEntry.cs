namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Entry data for a Set Variable function call
    /// </summary>
    public class ScriptFunctionCallSelfRegisterEntry : ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// Description, optional
        /// </summary>
        public string? Description { get; set; }
    }
}