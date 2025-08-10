namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Check HTTP Connection
    /// 
    /// This action determines whether a given URL is valid by using WinSock.dll to try to
    /// download the HTML page.
    /// 
    /// If the installation is not true 32-bit, specify both Win16 and Win32 error variables. Then,
    /// the Win32 WinSock.dll is used, followed by the Win16 WinSock.dll. Otherwise, only the
    /// 32-bit version is used.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f38".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class CheckHttpConnection : FunctionData
    {
        /// <summary>
        /// URL to check
        /// </summary>
        /// <remarks>Includes http://</remarks>
        public string? UrlToCheck { get; set; }

        /// <summary>
        /// 32-bit windosck.dll error text return
        /// </summary>
        public string? Win32ErrorTextVariable { get; set; }

        /// <summary>
        /// 32-bit winsock.dll error code return
        /// </summary>
        public string? Win32ErrorNumberVariable { get; set; }

        /// <summary>
        /// 16-bit windosck.dll error text return
        /// </summary>
        public string? Win16ErrorTextVariable { get; set; }

        /// <summary>
        /// 16-bit winsock.dll error code return
        /// </summary>
        public string? Win16ErrorNumberVariable { get; set; }
    }
}