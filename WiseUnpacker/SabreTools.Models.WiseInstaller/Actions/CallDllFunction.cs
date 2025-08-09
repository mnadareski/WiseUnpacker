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
        /// - Hide progress bar before calling function (0x04)
        /// - Unknown (0x08) - Unset if external library call?
        /// - Unknown (0x10) - Results in the flag being (^ 0x30)
        /// - Unknown (0x20) - Checked for existence along with 0x08
        ///     not existing to run a function. It also results
        ///     in the flag value being (^ 0x30)
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
        public FunctionData[]? Entries { get; set; }
    }
}
