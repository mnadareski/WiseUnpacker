namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Exit Installation
    /// 
    /// This action exits the installation.
    /// 
    /// No message appears unless you also set the RESTART variable.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f28".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ExitInstallation : FunctionData
    {
        // There is no data
    }
}