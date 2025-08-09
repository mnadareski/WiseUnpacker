namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Copy Local File
    /// 
    /// This action copies uncompressed files from a floppy disk, CD, the destination computer,
    /// or a network drive.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class CopyLocalFile : MachineStateData
    {
        /// <summary>
        /// Unknown, 0x0C
        /// </summary>
        public ushort Flags { get; set; } // 0x00 - 0x01

        /// <summary>
        /// Padding
        /// </summary>
        /// <remarks>
        /// 40 bytes, padding because structure is internally
        /// shared with <see cref="InstallFile"/> 
        /// </remarks>
        public byte[]? Padding { get; set; } // 0x02 - 0x2A

        /// <summary>
        /// Destination path
        /// </summary>
        public string? Destination { get; set; } // 0x2B - ?

        /// <summary>
        /// Description, one per language + 1
        /// </summary>
        public string[]? Description { get; set; }

        /// <summary>
        /// Source file
        /// </summary>
        public string? Source { get; set; }
    }
}
