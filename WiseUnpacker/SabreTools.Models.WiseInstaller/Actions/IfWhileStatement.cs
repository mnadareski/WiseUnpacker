namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// If Statement / While Statement
    /// 
    /// This action marks the beginning of a conditional block of script, an If block. If the
    /// condition specified in the If Statement is true, the lines inside the If block are executed.
    /// The If block can also contain an Else or several ElseIf actions.
    /// 
    /// This action begins a While loop. An End Statement must end the loop. As long as the
    /// condition specified in the While Statement Settings dialog box is true, the script lines
    /// inside the loop execute repeatedly. If the condition is not true, then the While loop is
    /// exited, and the next script line is executed.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class IfWhileStatement : MachineStateData
    {
        /// <summary>
        /// Flags, unknown values
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - If or While loop (Possibly 0x10 == While, 0x20 == If)
        /// - Perform While loop at least once (Possibly 0x40)
        /// - Operation (Between 0x00 and 0x0F)
        ///     + Addition (unknown)
        ///     + Subtraction (unknown)
        ///     + Multiplication (unknown)
        ///     + Division (unknown)
        ///     + Left (unknown)
        ///     + Right (unknown)
        ///     + Mid (unknown)
        ///     + Concat (unknown)
        ///     + Instr (unknown)
        ///     + Before (unknown)
        ///     + After (unknown)
        ///     + Len (Possibly 0x0D)
        ///     + Lcase (unknown)
        ///     + Ucase (unknown)
        ///     + Ltrim (unknown)
        ///     + Rtrim (unknown)
        ///     + And (unknown)
        ///     + Or (unknown)
        ///     + Not (unknown)
        ///     + > (unknown)
        ///     + < (unknown)
        ///     + >= (unknown)
        ///     + <= (unknown)
        ///     + = (unknown)
        ///     + <> (unknown)
        /// 
        /// If the Flags & 0x20 == 0 and Flags & 0x10 == 0,
        /// the flag value in the stack is set back to
        /// Flags ^ 0x60. 
        /// </remarks>
        public byte Flags { get; set; }

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
