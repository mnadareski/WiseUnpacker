namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Add to SYSTEM.INI
    /// 
    /// (Windows 3.1x or Windows 9x only) This action adds a device entry to the 386Enh
    /// section of the System.ini file. The destination computer is restarted automatically to
    /// force the new device driver to be loaded.
    /// 
    /// Do not use this action to modify the display driver (display=xxx) or any other non-
    /// device entry. Instead, use the Edit INI action.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f3".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class AddToSystemIni : FunctionData
    {
        /// <summary>
        /// Full commandline for the device
        /// </summary>
        public string? DeviceName { get; set; }
    }
}