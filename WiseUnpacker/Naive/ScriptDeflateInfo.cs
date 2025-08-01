namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Deflated Wise file?
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptDeflateInfo
    {
        /// <summary>
        /// Start of the deflated data
        /// </summary>
        public uint DeflateStart { get; set; }

        /// <summary>
        /// End of the deflated data
        /// </summary>
        public uint DeflateEnd { get; set; }

        /// <summary>
        /// Inflated data size
        /// </summary>
        public uint InflatedSize { get; set; }
    }
}
