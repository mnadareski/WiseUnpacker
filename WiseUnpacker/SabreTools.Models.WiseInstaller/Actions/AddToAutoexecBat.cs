namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Add to AUTOEXEC.BAT
    /// 
    /// This action edits Autoexec.bat, which is executed during startup, allowing you to add
    /// commands that are executed before Windows loads.
    /// 
    /// Insert commands at a particular line number, or search the file for specific text and
    /// insert the new line before, after, or in place of the existing line. The destination
    /// computer is restarted after installation to force the new commands to take effect.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f1".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class AddToAutoexecBat : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Case Sensitive (0x01)
        /// - Insert Action (unknown)
        /// - Match Criteria (unknown)
        /// - Ignore White Space (unknown)
        /// - Make Backup File (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Full path to the text file to edit
        /// </summary>
        /// <remarks>
        /// Ignored because structure is shared with both <see cref="AddToConfigSys"/>
        /// and <see cref="InsertLineIntoTextFile"/>
        /// </remarks>
        public string? FileToEdit { get; set; }

        /// <summary>
        /// Text to insert into the file
        /// </summary>
        public string? TextToInsert { get; set; }

        /// <summary>
        /// Search for Text
        /// </summary>
        public string? SearchForText { get; set; }

        /// <summary>
        /// Comment Text
        /// </summary>
        public string? CommentText { get; set; }

        /// <summary>
        /// Line number to insert text at, 0 for append to end
        /// </summary>
        public int LineNumber { get; set; }
    }
}