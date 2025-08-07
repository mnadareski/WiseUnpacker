namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// No-op
    /// 
    /// This operation was found in WISE0001.DLL and appears to
    /// do nothing except move to the next operation.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class NoOp : MachineStateData
    {
        // There is no data
    }
}
