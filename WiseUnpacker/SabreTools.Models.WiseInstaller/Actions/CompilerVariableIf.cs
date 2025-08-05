namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Compiler Variable If
    /// 
    /// Compiler Variable If, Else, and End actions are used in an If block to let you compile
    /// different versions of an installation. You set the value of a compiler variable at compile
    /// time, and the actions inside a compiler variable If block are added to the script
    /// according to the value of the compiler variable.
    /// 
    /// You create compiler variables on the Compiler Variables page. When you create a
    /// compiler variable, you specify its default value. You also specify when you should be
    /// prompted for a compiler variable value.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class CompilerVariableIf : MachineStateData
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
