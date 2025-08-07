namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Include Script
    /// 
    /// This action adds an additional script to the current installation script. During compile,
    /// the include script is copied into the calling script at the location of the Include Script
    /// action, resulting in a combination of the scripts.
    /// 
    /// Include scripts can save time because you can develop a library of WiseScripts that
    /// perform specific functions, like subroutines. You can re-use include scripts and share
    /// them with colleagues. They typically contain just a few lines of code, such as calling an
    /// .EXE or displaying a particular dialog box. Include scripts can be any size with the
    /// limitation that the calling script plus include scripts cannot be more than 32,000 lines.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class IncludeScript : MachineStateData
    {
        /// <summary>
        /// Count of sequential 0x1B bytes, excluding the original opcode
        /// </summary>
        public int Count { get; set; }
    }
}
