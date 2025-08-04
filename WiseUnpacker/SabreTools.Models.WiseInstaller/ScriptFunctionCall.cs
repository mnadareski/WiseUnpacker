namespace SabreTools.Models.WiseInstaller
{
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptFunctionCall : MachineStateData
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public byte Operand_1 { get; set; }

        /// <summary>
        /// DLL path/name or NULL for Wise internal
        /// </summary>
        public string? DllPath { get; set; }

        /// <summary>
        /// Function name
        /// </summary>
        /// <remarks>
        /// f8 - Read INI Value
        /// f9 - Get Registry Key Value
        /// f12 - Check Configuration -- Acts like an IF/THEN
        /// f13 - ???? [Included in DetectCookie.wse]
        /// f16 - Set Variable
        /// f17 - Get Environment Variable
        /// f19 - Check if File/Dir Exists
        /// f23 - Add ProgMan Icon(?) [Included in uninstal.wse]
        /// f27 - Parse String
        /// f29 - Self-Register OCXs/DLLs
        /// f31 - Wizard Block
        /// f33 - ???? [Included in DetectCookie.wse]
        /// f34 - Post to HTTP Server
        /// [External] - Call DLL Function
        /// </remarks>
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
        public string[]? Entries { get; set; }
    }
}
