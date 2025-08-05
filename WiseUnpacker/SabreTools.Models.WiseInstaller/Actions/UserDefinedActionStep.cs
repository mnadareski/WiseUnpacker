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
        /// Indicates the number of lines in the script
        /// </summary>
        /// <remarks>Always 0 after the first step</remarks>
        public byte Count { get; set; }

        /// <summary>
        /// Script lines
        /// </summary>
        /// <remarks>One string per language</remarks>
        public string[]? ScriptLines { get; set; }
    }
}
