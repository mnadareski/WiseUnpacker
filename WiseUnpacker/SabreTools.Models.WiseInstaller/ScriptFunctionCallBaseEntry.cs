namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// A single set of flags, message, and title for a function call
    /// </summary>
    /// <remarks>
    /// Parts of the function call entry are split by 0x7F. It appears
    /// that the minimum function call entry has a set of flags only.
    /// </remarks>
    public abstract class ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// Flags
        /// </summary>
        /// <remarks>Encoded as a string, binary representation in script file</remarks>
        public byte Flags { get; set; }
    }
}