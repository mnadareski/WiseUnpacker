namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// 'else if' struct, same as the 0x0C struct, but handled differently
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptUnknown0x23 : MachineStateData
    {
        /// <summary>
        /// Unknown, operator?
        /// </summary>
        public byte Unknown_1 { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string? VarName { get; set; }

        /// <summary>
        /// Variable value
        /// </summary>
        public string? VarValue { get; set; }
    }
}
