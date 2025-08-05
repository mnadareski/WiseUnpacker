namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Display Message
    /// 
    /// This action displays a message dialog box and can optionally branch the script based on
    /// the end user response. Without the branching option, this dialog box has an OK button,
    /// which continues, and a Cancel button, which halts installation.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class DisplayMessage : MachineStateData
    {
        /// <summary>
        /// Flags, unknown mapping
        /// </summary>
        /// <remarks>
        /// Expected flags:
        /// - Message icon(?)
        /// - Start If Block (unknown)
        /// - No Cancel (unknown)
        /// </remarks>
        public byte Flags { get; set; }

        /// <summary>
        /// Strings, two per language (1 title and 1 message)
        /// </summary>
        public string[]? TitleText { get; set; }
    }
}
