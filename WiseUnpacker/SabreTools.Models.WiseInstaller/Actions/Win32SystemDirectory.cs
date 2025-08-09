namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Win32 System Directory
    /// 
    /// This action puts the path to the operating system directory (%WIN%\System32) into a
    /// variable. Alternatively, use the predefined variables %SYS% or %SYS32% to access the
    /// system directory. This action is included to provide backward compatibility for older
    /// WiseScripts.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f11".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class Win32SystemDirectory : FunctionData
    {
        /// <summary>
        /// Variable name to store the value in
        /// </summary>
        public string? VariableName { get; set; }
    }
}