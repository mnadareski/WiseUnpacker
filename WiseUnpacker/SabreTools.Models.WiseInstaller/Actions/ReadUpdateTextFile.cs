namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Read/Update Text File
    /// 
    /// This action begins a loop that reads and, optionally, updates text in a text file. Each loop
    /// puts the next line of text into a variable. You can put actions in the loop that change the
    /// contents of the variable (example: Parse String). Optionally, the changed variable can
    /// be written back to the file. The loop repeats for each line of the file. This action requires
    /// an End Statement.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f33".
    /// This acts like the start of a block.
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ReadUpdateTextFile
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Read lines of file into variable (unknown)
        /// - Update file with new contents of variable (unknown)
        /// - Make Backup File (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Variable to store each line of the text file
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Full path to the text file to be edited
        /// </summary>
        public string? Pathname { get; set; }

        /// <summary>
        /// Unknown data, 0x20-filled in sample calls
        /// </summary>
        public string? Unknown { get; set; }
    }
}