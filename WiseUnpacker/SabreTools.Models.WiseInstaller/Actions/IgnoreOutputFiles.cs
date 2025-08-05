namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Ignore Output Files
    /// </summary>
    /// <remarks>
    /// This seems to indicate files that are ignored or otherwise
    /// not copied from the source. It has been observed before
    /// link files are created and in the middle of large chains
    /// of install file calls. Unfortunately, the only instance
    /// where this could be found in a source script does not give
    /// any indication as to why the paths in question are being
    /// ignored.
    /// 
    /// From running an installer that contained these, the files
    /// in question were omitted from install. It's unclear if those
    /// files are included in the installer and hidden or if they
    /// are omitted during compilation.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class IgnoreOutputFiles : MachineStateData
    {
        /// <summary>
        /// Pathname to ignore
        /// </summary>
        /// <remarks>Can contain wildcards</remarks>
        public string? Pathname { get; set; }
    }
}
