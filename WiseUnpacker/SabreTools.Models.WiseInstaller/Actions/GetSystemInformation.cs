namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Get System Information
    /// 
    /// This action retrieves information about the destination computer and puts it into a
    /// variable.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class GetSystemInformation : MachineStateData
    {
        /// <summary>
        /// Flags, values unknown
        /// </summary>
        /// <remarks>
        /// Expected flags:
        /// - Retrieve Current Date/Time (unknown)
        /// - Windows Version (unknown)
        /// - DOS Version (unknown)
        /// - K Bytes Physical Memory (unknown)
        /// - File Date/Time Modified (unknown)
        /// - File Version Number (unknown)
        /// - Registered Owner Name (unknown)
        /// - Registered Company Name (unknown)
        /// - Drive Type for Pathname (unknown)
        /// - First Network Drive (unknown)
        /// - First CD-ROM Drive (unknown)
        /// - Win32s Version (unknown)
        /// - Full UNC Pathname
        /// - Installer EXE Pathname
        /// - File Size (Bytes)
        /// - Volume Serial Number
        /// - Volume Label
        /// - Windows Logon Name
        /// - Service Pack Number
        /// - Current Date/Time (four-digit year)
        /// - File Date/Time Modified (four-digit year)
        /// - Disk Free Space (KBytes)
        /// - Current Date/Time (Regional settings)
        /// - UTC File Date/Time Modified
        /// - Is OS 64 Bit
        /// </remarks>
        public byte Flags { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Used only for operations that retrieve information
        /// on files or directories
        /// </summary>
        public string? Pathname { get; set; }
    }
}
