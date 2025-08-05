namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Get Temporary Filename
    /// 
    /// This action generates a unique, temporary file name and stores it in a variable. Use the
    /// temporary name when you need to install a file to the Windows Temp directory
    /// (%TEMP%). Files that you create using this file name are deleted when the installation
    /// finishes. Example: Use this to install a .DLL that is called during installation, and is then
    /// no longer needed.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class GetTemporaryFilename : MachineStateData
    {
        /// <summary>
        /// Variable to store the temporary file name
        /// </summary>
        public string? Variable { get; set; }
    }
}
