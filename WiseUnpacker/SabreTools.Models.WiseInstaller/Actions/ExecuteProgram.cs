namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Execute Program
    /// 
    /// This action runs another .EXE. The .EXE can be a file that is already installed on the
    /// destination computer, a file you installed as part of the installation, or a file you provide
    /// on a separate disk.
    /// 
    /// If the .EXE you plan to execute is coded to pass back a return value, the resulting return
    /// value is put into the variables %INSTALL_RESULT% and %PROCEXITCODE%. If the
    /// EXE passes back a return value, mark the Wait for Program to Exit check box.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ExecuteProgram : MachineStateData
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
