namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Create Directory
    /// 
    /// Directories are created when files are installed to them. Use this action only to create an
    /// empty directory on the destination computer.
    /// 
    /// When a WiseScript is called by a Windows Installer installation, you also can create a
    /// directory on the Features or Components tabs of Setup Editor in Windows Installer
    /// Editor.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class CreateDirectory : MachineStateData
    {
        /// <summary>
        /// Pathname of the directory to create
        /// </summary>
        /// <remarks>Should start with a variable</remarks>
        public string? Pathname { get; set; }
    }
}
