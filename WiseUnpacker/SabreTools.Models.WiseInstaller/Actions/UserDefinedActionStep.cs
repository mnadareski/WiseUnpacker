namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// User-Defined Action Step
    /// 
    /// You create a user-defined action by creating a separate WiseScript and saving it in the
    /// Actions subdirectory of this product’s installation directory, or in the shared directory
    /// that is specified in Preferences.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class UserDefinedActionStep : MachineStateData
    {
        /// <summary>
        /// Flags for writing out
        /// </summary>
        /// <remarks>
        /// Values:
        /// - 0x01 - Used as value appended to the end of the selected line
        /// - 0x02 - Indicates if the string should be formatted(?)
        /// </remarks>
        public byte Flags { get; set; }

        /// <summary>
        /// Script lines
        /// </summary>
        public string[]? ScriptLines { get; set; }
    }
}
