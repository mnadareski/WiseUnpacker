namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Unknown
    /// </summary>
    /// <remarks>
    /// This action enables a flag that is used by this and <see cref="Unknown0x24"/>.
    /// It seems to only be referenced in contexts where there are registry
    /// keys read and written, specifically about repair.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class Unknown0x25 : MachineStateData
    {
        // There is no data
    }
}