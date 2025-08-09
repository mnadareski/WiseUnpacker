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
    public class SetVariable : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// (flags >> 2 & 0x0F)
        /// - Nothing (0x00)
        /// - Increment (0x04)
        /// - Decrement (0x08)
        /// - Remove trailing backslashes (0x0C)
        /// - Convert to long filename (0x10)
        /// - Convert to short filename (0x14)
        /// - Convert to uppercase (0x18)
        /// - Convert to lowercase (0x1C)
        /// 
        /// One of the following is case 0x20:
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