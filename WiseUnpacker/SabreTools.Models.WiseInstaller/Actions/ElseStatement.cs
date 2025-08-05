namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Else Statement
    /// 
    /// This action marks the beginning of a section of instructions to be executed when the
    /// condition specified in the matching If action is false. It takes no parameters, and
    /// selecting it from the Actions list inserts it directly into the script with no further dialog
    /// boxes or prompts.
    /// </summary>
    /// <remarks>
    /// This acts like the start of a block.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ElseStatement : MachineStateData
    {
        // There is no data
    }
}
