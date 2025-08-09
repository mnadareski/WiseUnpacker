namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Unknown
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f30".
    /// 
    /// Maybe "Modify Component Size"?
    /// 
    /// Probably this layout:
    /// - Flags (numeric)
    /// - Directory name (e.g. "%INST%\..\DirectX")
    /// - File name (e.g. "%INST%\..\DirectX\DSETUP.DLL")
    /// - Offset? (e.g. "2623")
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class UnknownF30 : FunctionData
    {
        /// <summary>
        /// Arguments passed in from the Call DLL Function
        /// </summary>
        public string[]? Args { get; set; }
    }
}