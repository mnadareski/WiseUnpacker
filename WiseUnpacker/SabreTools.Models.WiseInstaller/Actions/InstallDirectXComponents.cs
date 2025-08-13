namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Install DirectX Components
    /// 
    /// Little is known about this function other than it seems to install any
    /// local DirectX components, if necessary. No official documentation
    /// is publicly available that contains a reference to this.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f30".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class InstallDirectXComponents : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - 0x40 - Unsets itself and sets 0x200 if not already
        /// - If final value & 0x3fffff7f == 0, return failure
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Root path containing all DirectX components
        /// </summary>
        public string? RootPath { get; set; }

        /// <summary>
        /// Path to the DSETUP.DLL to be used
        /// </summary>
        public string? LibraryPath { get; set; }

        /// <summary>
        /// Unknown numeric value
        /// </summary>
        /// <remarks>Not fully identified; replaces the flags if not 0?</remarks>
        public int SizeOrOffsetOrFlag { get; set; }
    }
}