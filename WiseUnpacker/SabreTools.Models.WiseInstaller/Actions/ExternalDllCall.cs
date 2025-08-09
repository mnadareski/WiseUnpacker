namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// External DLL Call
    /// 
    /// This represents a call to an external DLL.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is invoked when
    /// the external DLL path is provided.
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ExternalDllCall : FunctionData
    {
        /// <summary>
        /// Arguments passed in from the Call DLL Function
        /// </summary>
        public string[]? Args { get; set; }
    }
}