namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// If statement comparing against a compiler variable
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptCompilerVariableIf : MachineStateData
    {
        /// <summary>
        /// Flags, unknown values
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }
    }
}
