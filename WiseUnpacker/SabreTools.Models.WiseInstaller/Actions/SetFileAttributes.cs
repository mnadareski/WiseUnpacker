namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Set File Attributes
    /// 
    /// This action sets the attributes of one file or a group of files.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f12".
    /// This acts like the start of a block if a flag is set.
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class SetFileAttributes : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Read Only (Maybe 0x01)
        /// - Hidden (Maybe 0x02)
        /// - System (Maybe 0x04)
        /// - Scan Directory Tree (unknown)
        /// - Archive (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// File to change
        /// </summary>
        /// <remarks>Wildcards are allowed</remarks>
        public string? FilePathname { get; set; }
    }
}