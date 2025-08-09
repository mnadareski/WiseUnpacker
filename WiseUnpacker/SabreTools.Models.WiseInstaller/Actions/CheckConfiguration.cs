namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Check Configuration
    /// 
    /// This action tests the hardware configuration, operating system, and other characteristics
    /// of the destination computer. As a result of this check, the action can display a message,
    /// halt the installation after displaying a message, or start a conditional block.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f12".
    /// This acts like the start of a block if a flag is set.
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class CheckConfiguration : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - System (unknown)
        /// - Display Message Only (unknown)
        /// - Abort Installation (unknown)
        /// - Start Block (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Appears in the body of the message dialog box. Leave this blank to prevent the
        /// message from appearing.
        /// </summary>
        /// <remarks>If fully numeric, it was probably called Flags2 in the source</remarks>
        public string? Message { get; set; }

        /// <summary>
        /// Dialog box title
        /// </summary>
        public string? Title { get; set; }
    }
}