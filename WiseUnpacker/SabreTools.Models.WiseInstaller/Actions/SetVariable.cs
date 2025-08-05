namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Set Variable
    /// 
    /// This action sets the value of a variable by providing a literal value, by modifying the
    /// variable’s existing value, or by evaluating an expression.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f16".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class SetVariable
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Nothing (unknown)
        /// - Increment (unknown)
        /// - Decrement (unknown)
        /// - Remove trailing backslashes (unknown)
        /// - Convert to long filename (unknown)
        /// - Convert to short filename (unknown)
        /// - Convert to uppercase (unknown)
        /// - Convert to lowercase (unknown)
        /// - Evaluate Expression (unknown)
        /// - Append to Existing Value (unknown)
        /// - Remove File Name (unknown)
        /// - Read Variable From Values File (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

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