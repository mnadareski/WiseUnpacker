namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Read/Write Binary File
    /// 
    /// This action reads from a binary file to a variable, or writes from a variable to a binary
    /// file. If you write to the file, the existing information in the file is not moved, it is
    /// overwritten.
    /// 
    /// This action does not support reading or writing non-ASCII characters (characters with
    /// codes above 127).
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f15".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ReadWriteBinaryFile : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Transfer Direction [0x00 == Read, 0x01 == Write]
        /// - Null Terminated (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Full path to the binary file
        /// </summary>
        public string? FilePathname { get; set; }

        /// <summary>
        /// Name of the variable
        /// </summary>
        public string? VariableName { get; set; }

        /// <summary>
        /// Offset in the file to start from
        /// </summary>
        /// <remarks>Encoded as a string</remarks>
        public int FileOffset { get; set; }

        /// <summary>
        /// Maximum number of bytes to process
        /// </summary>
        /// <remarks>Encoded as a string</remarks>
        public int MaxLength { get; set; }
    }
}