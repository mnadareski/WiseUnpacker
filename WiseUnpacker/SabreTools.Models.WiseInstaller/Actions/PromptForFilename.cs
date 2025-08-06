namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Prompt for Filename
    /// 
    /// This action prompts the end user to select a file using a standard Open or Save dialog
    /// box. The complete path of the file or directory is returned in a variable. (Example: Use
    /// the returned directory to set the installation directory for a subset of files.) No file is
    /// actually opened or saved by this action. This action is included to provide backward
    /// compatibility for older WiseScripts. In new scripts, use custom dialog boxes or dialog
    /// box controls to perform the same function. This action requires an End Statement,
    /// because it begins a block of statements, similar to an If Statement.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f35".
    /// This acts like the start of a block.
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class PromptForFilename
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Dialog Type (unknown)
        /// - Allow selection of multiple files (unknown)
        /// - Prompt if file does not exist (unknown)
        /// - File must exist (unknown)
        /// - Pathname must exist (unknown)
        /// - Skip write permissions test (unknown)
        /// - Do not validate the pathname (unknown)
        /// - Display prompt if overwriting existing file (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Destination variable for the selection
        /// </summary>
        public string? DestinationVariable { get; set; }

        /// <summary>
        /// Default extension if one is not entered
        /// </summary>
        public string? DefaultExtension { get; set; }

        /// <summary>
        /// Title for the dialog box
        /// </summary>
        public string? DialogTitle { get; set; }

        /// <summary>
        /// File types to appear in the selction list
        /// </summary>
        /// <remarks>
        /// Formatted like "File Description (*.ext);*.ext"
        /// </remarks>
        public string? FilterList { get; set; }
    }
}