namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Wise script file
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptFile
    {
        /// <summary>
        /// Script header
        /// </summary>
        public ScriptHeader? Header { get; set; }

        /// <summary>
        /// States representing the state machine in order
        /// </summary>
        public MachineState[]? States { get; set; }
    }
}