namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Execute Program
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptExecuteProgram : MachineStateData
    {
        /// <summary>
        /// Flags, unknown values
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// Path to the program to execute
        /// </summary>
        public string? Pathname { get; set; }

        /// <summary>
        /// Command Line
        /// </summary>
        public string? CommandLine { get; set; }

        /// <summary>
        /// Default directory name
        /// </summary>
        public string? DefaultDirectory { get; set; }
    }
}
