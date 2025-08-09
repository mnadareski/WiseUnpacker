namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Invalid Operation
    /// 
    /// This operation was found in WISE0001.DLL and appears to
    /// abort installation with a return code of 0xfffffffe.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class InvalidOperation : MachineStateData
    {
        // There is no data
    }
}
