namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Add Directory to PATH
    /// 
    /// This action adds a directory to the PATH environment variable, as set in Autoexec.bat.
    /// The directory is appended to every occurrence of the SET PATH statement that does not
    /// already contain it. A SET PATH statement is added if none exists. The system restarts at
    /// the end of installation so that the new PATH takes effect.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f0".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class AddDirectoryToPath : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Location to add [start of PATH or end of PATH] (unknown)
        ///     + No indication that this flag is respected, if it exists
        /// - Add to all PATH variables (unknown)
        ///     + Only seems to apply to AUTOEXEC.BAT-based paths?
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Directory name to add to path
        /// </summary>
        /// <remarks>May contain a variable name</remarks>
        public string? Directory { get; set; }
    }
}