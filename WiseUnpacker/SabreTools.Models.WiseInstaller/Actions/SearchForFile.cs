namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Search for File
    /// 
    /// This action searches for a file on local drives, network drives, or all drives, and returns
    /// the full path to the file.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f13".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class SearchForFile
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Return Type (unknown)
        /// - Drives to Search (local, network, both) (unknown)
        /// - Search Depth (unknown)
        /// - Remove File Name (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Variable to store the file path
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Filename to search for
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Default value if the file is not found
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Message to display during the search operation
        /// </summary>
        public string? MessageText { get; set; }
    }
}