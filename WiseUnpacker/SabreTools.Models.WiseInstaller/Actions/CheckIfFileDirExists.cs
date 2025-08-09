namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Check If File/Dir Exists
    /// 
    /// This action determines if a file or directory exists, whether a directory is writable, or if a
    /// .DLL is loaded into memory. It can perform different actions based on the result of the
    /// check.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f19".
    /// This acts like the start of a block if a flag is set.
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class CheckIfFileDirExists : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Display Message Only (unknown)
        /// - Abort Installation (unknown)
        /// - Start Block (unknown)
        /// - Start While Loop (unknown)
        /// - Perform loop at least once (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Pathname
        /// </summary>
        public string? Pathname { get; set; }

        /// <summary>
        /// Appears in the body of the message dialog box. Leave this blank to prevent the
        /// message from appearing.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Dialog box title
        /// </summary>
        public string? Title { get; set; }
    }
}