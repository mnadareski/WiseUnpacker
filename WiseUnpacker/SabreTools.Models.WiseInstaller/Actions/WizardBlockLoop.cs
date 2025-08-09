namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Wizard Block / Wizard Loop
    /// 
    /// This action precedes dialog boxes that make up the majority of the installation’s end
    /// user interface. End users can move forward and backward through these dialog boxes.
    /// The script continues executing inside the wizard loop until the last dialog box has been
    /// completed and accepted.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f31".
    /// This acts like the start of a block if a flag is set(?).
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class WizardBlockLoop : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Skip Dialog (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        // TODO: Below is actually a list of dialog boxes
        // TODO: Below should move to create dialog box

        /// <summary>
        /// Direction variable name
        /// </summary>
        /// </remarks>"B" == Previous, "N" == Next</remarks>
        public string? DirectionVariable { get; set; }

        /// <summary>
        /// Display variable name
        /// </summary>
        public string? DisplayVariable { get; set; }

        /// <summary>
        /// X Position
        /// </summary>
        public int? XPosition { get; set; }

        /// <summary>
        /// Y Position, numeric
        /// </summary>
        public int? YPosition { get; set; }

        /// <summary>
        /// Filler Color, numeric
        /// </summary>
        public int? FillerColor { get; set; }

        /// <summary>
        /// Unknown, numeric
        /// </summary>
        public string? Operand_6 { get; set; }

        /// <summary>
        /// Unknown, numeric
        /// </summary>
        public string? Operand_7 { get; set; }

        /// <summary>
        /// Unknown, numeric
        /// </summary>
        public string? Operand_8 { get; set; }

        /// <summary>
        /// Either just a dialog name or a combination of
        /// Dialog, Variable, Value, and Compare values
        /// that are `\t` separated. Dialog and Variable
        /// are both strings, Value and Compare both seem
        /// to be numeric
        /// </summary>
        public string? DialogVariableValueCompare { get; set; }
    }
}