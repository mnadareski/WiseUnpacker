namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Unknown
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f0".
    /// 
    /// Could be either:
    /// - Set Control Text
    /// - Set Current Control
    /// 
    /// Layout is unknown
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class UnknownF0 : FunctionData
    {
        /// <summary>
        /// Arguments passed in from the Call DLL Function
        /// </summary>
        public string[]? Args { get; set; }
    }
}