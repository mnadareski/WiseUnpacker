namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Delete File
    /// 
    /// This action removes files from the destination computer.
    /// 
    /// You do not need to delete temp files if you use the Get Temporary Filename action to
    /// create them because they are deleted automatically.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class DeleteFile : MachineStateData
    {
        /// <summary>
        /// Flags, unknown values
        /// </summary>
        /// <remarks>
        /// Expected flags:
        /// - Include Sub-Directories (unknown)
        /// - Remove Directory Containing Files (unknown)
        /// </remarks>
        public byte Flags { get; set; }

        /// <summary>
        /// Path name
        /// </summary>
        public string? Pathname { get; set; }
    }
}
