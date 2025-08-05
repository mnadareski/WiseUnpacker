namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Rename File/Directory
    /// 
    /// This action renames a file or directory on the destination computer. This can be an
    /// existing file or directory, or a file or directory that your installation installed. The file
    /// must not be busy.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class RenameFileDirectory : MachineStateData
    {
        /// <summary>
        /// Full path to the existing file or directory
        /// </summary>
        public string? OldPathname { get; set; }

        /// <summary>
        /// New file or directory name
        /// </summary>
        public string? NewFileName { get; set; }
    }
}
