namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// ElseIf Statement
    /// 
    /// This action is put inside an If block to check for another condition. It marks the
    /// beginning of a block of code that is executed only if the condition checked by the If
    /// Statement is false, all previous ElseIfs are false, and this ElseIf is true. You can use one
    /// If Statement with multiple ElseIf Statements to check for multiple conditions.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ElseIfStatement : MachineStateData
    {
        /// <summary>
        /// Operator, values need to be mapped
        /// </summary>
        public byte Operator { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string? Value { get; set; }
    }
}
