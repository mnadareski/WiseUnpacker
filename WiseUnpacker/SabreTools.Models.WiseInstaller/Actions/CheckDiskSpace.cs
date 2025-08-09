namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Check Disk Space
    /// 
    /// This action determines if enough disk space is available for the installation, based on
    /// files that are always installed. You would use this action only if the WiseScript contains
    /// Install File(s) actions that install files permanently on the destination computer.
    /// 
    /// You can leave all fields blank and the action checks disk space for all files. This action
    /// takes the cluster size of the disk into account.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f23".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class CheckDiskSpace : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Do not cancel during silent installation (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Required disk space for up to 3 additional disks
        /// </summary>
        public string? ReserveSpace { get; set; }

        /// <summary>
        /// Variable to store the result of the space check
        /// </summary>
        public string? StatusVariable { get; set; }

        /// <summary>
        /// Component variables
        /// </summary>
        public string? ComponentVariables { get; set; }
    }
}