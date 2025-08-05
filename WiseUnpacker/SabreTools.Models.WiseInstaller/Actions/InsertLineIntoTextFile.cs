namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Insert Line Into Text File
    /// 
    /// This action edits a text file on the destination computer. Use it to edit configuration files
    /// that cannot be edited by Edit INI File, Add Device to System.ini, Add Command to
    /// Config.sys, or Add Command to Autoexec.bat.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f25".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class InsertLineIntoTextFile
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Insert Action (unknown)
        /// - Match Criteria (unknown)
        /// - Ignore White Space (unknown)
        /// - Case Sensitive (unknown)
        /// - Make Backup File (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Full path to the text file to edit
        /// </summary>
        public string? FileToEdit { get; set; }

        /// <summary>
        /// Text to insert into the file
        /// </summary>
        public string? TextToInsert { get; set; }

        /// <summary>
        /// Search for Text / Comment Text
        /// </summary>
        public string? UnknownString_1 { get; set; }

        /// <summary>
        /// Search for Text / Comment Text
        /// </summary>
        public string? UnknownString_2 { get; set; }

        /// <summary>
        /// Line number to insert text at, 0 for append to end
        /// </summary>
        public int LineNumber { get; set; }
    }
}