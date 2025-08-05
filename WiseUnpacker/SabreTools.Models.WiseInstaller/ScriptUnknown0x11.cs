namespace SabreTools.Models.WiseInstaller
{
    /// <remarks>
    /// This seems to indicate files that are ignored or otherwise
    /// not copied from the source. It has been observed before
    /// link files are created and in the middle of large chains
    /// of install file calls. Unfortunately, the only instance
    /// where this could be found in a source script does not give
    /// any indication as to why the paths in question are being
    /// ignored.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ScriptUnknown0x11 : MachineStateData
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public string? Operand_1 { get; set; }
    }
}
