namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Call DLL Function
    /// 
    /// This action calls a .DLL function from a .DLL on the destination computer. They can be
    /// be .DLLs you have written, .DLLs developed for WiseScript, or Windows .DLLs. You can
    /// branch the script based on the returned results of a .DLL by setting the Action to Start
    /// Block if Return Value True or Start While Loop.
    /// </summary>
    /// <remarks>
    /// This acts like the start of a block if a flag is set.
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class CallDllFunction : MachineStateData
    {
        /// <summary>
        /// Flags, unknown mapping
        /// </summary>
        /// <remarks>
        /// Expected Values:
        /// - Start block if function returns true (0x02 or 0x03)
        /// - Loop while function returns true (0x02 or 0x03)
        /// </remarks>
        public byte Flags { get; set; }

        /// <summary>
        /// DLL path/name or NULL for Wise internal
        /// </summary>
        public string? DllPath { get; set; }

        /// <summary>
        /// Function name
        /// </summary>
        public string? FunctionName { get; set; }

        /// <summary>
        /// Args?
        /// </summary>
        /// <remarks>In older/trimmed scripts, this seems to be missing?</remarks>
        public string? Operand_4 { get; set; }

        /// <summary>
        /// Return variable from an external call
        /// </summary>
        /// <remarks>In older/trimmed scripts, this seems to be missing?</remarks>
        public string? ReturnVariable { get; set; }

        /// <summary>
        /// One entry per language count
        /// </summary>
        /// <remarks>
        /// TODO: Figure out if it's more appropriate to store
        /// the string data in its unparsed form or as the concrete
        /// class data, where possible.
        /// </remarks>
        public string[]? Entries { get; set; }
    }
}
