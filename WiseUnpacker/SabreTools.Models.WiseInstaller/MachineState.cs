namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Represents a single step in the state machine defined
    /// in a Wise script file.
    /// </summary>
    public class MachineState
    {
        /// <summary>
        /// Opcode
        /// </summary>
        public OperationCode Op { get; set; }

        /// <summary>
        /// Data specific to the operation, may be null
        /// </summary>

        public MachineStateData? Data { get; set; }
    }
}