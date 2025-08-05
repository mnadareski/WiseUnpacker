namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// End Block / End Statement
    /// 
    /// This action marks the end of an If block or a While loop. It takes no parameters, and
    /// selecting it from the Action list inserts it directly into the script with no further dialog
    /// boxes or prompts.
    /// </summary>
    /// <remarks>
    /// The documentation mentions that this statement should contain no data
    /// but there is still an operand that is apparently present. It is unknown what
    /// this value could map to.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class EndBlockStatement : MachineStateData
    {
        /// <summary>
        /// Unknown, maybe flags?
        /// </summary>
        /// <remarks>
        /// 0x00 - ???
        /// 0x01 - ???
        /// </remarks>
        public byte Operand_1 { get; set; }
    }
}
