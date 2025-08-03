namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Mark the end of a block
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptEndBlock : MachineStateData
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
