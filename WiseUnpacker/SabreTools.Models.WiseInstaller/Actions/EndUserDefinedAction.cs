namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// End User-Defined Action
    /// 
    /// You create a user-defined action by creating a separate WiseScript and saving it in the
    /// Actions subdirectory of this product’s installation directory, or in the shared directory
    /// that is specified in Preferences.
    /// </summary>
    /// <remarks>
    /// This acts like the end of a block.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class EndUserDefinedAction : MachineStateData
    {
        // There is no data
    }
}
