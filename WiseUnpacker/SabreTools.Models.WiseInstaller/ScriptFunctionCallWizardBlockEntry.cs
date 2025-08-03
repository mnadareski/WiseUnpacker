namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Entry data for a Wizard Block function call
    /// </summary>
    public class ScriptFunctionCallWizardBlockEntry : ScriptFunctionCallBaseEntry
    {
        /// <summary>
        /// Direction variable name
        /// </summary>
        public string? DirectionVariable { get; set; }

        /// <summary>
        /// Display variable name
        /// </summary>
        public string? DisplayVariable { get; set; }

        /// <summary>
        /// X Position, numeric
        /// </summary>
        public string? XPosition { get; set; }

        /// <summary>
        /// Y Position, numeric
        /// </summary>
        public string? YPosition { get; set; }

        /// <summary>
        /// Filler Color, numeric
        /// </summary>
        public string? FillerColor { get; set; }

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