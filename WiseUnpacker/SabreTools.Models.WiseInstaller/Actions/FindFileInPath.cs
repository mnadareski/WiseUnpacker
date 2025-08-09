namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Find File in Path
    /// 
    /// This action searches for a file on the destination computer. If more than one match
    /// exists, only the first match is returned.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f22".
    /// This acts like the start of a block if the default value is omitted(?)
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class FindFileInPath : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Remove File Name (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Variable to store the path if it is found
        /// </summary>
        public string? VariableName { get; set; }

        /// <summary>
        /// File name, not a full path
        /// </summary>
        /// <remarks>Wildcard characters are not allowed</remarks>
        public string? FileName { get; set; }

        /// <summary>
        /// Value to put in the variable if the file is not found
        /// </summary>
        /// <remarks>Leave blank to evaluage to false for an if</remarks>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Semicolon-delimited list of directories to search
        /// </summary>
        /// <remarks>Variables are allowed. If blank, PATH is used.</remarks>
        public string? SearchDirectories { get; set; }

        /// <summary>
        /// Optional description to display if the search takes longer than
        /// 1.5 seconds to complete.
        /// </summary>
        public string? Description { get; set; }
    }
}