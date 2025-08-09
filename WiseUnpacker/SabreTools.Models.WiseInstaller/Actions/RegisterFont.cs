namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Register Font
    /// 
    /// This action registers a new TrueType font (.TTF file) that has been copied into the
    /// Windows font directory.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f10".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class RegisterFont : FunctionData
    {
        /// <summary>
        /// TrueType font filename, not path
        /// </summary>
        public string? FontFileName { get; set; }

        /// <summary>
        /// Full name of the font
        /// </summary>
        public string? FontName { get; set; }
    }
}