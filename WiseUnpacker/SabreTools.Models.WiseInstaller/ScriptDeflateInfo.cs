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
        public uint DeflateStart { get; set; } // 0x00 - 0x03 (0x07 - 0x0A)

        /// <summary>
        /// End of the deflated data
        /// </summary>
        public uint DeflateEnd { get; set; } // 0x04 - 0x07 (0x0B - 0x0E)

        /// <summary>
        /// Inflated data size
        /// </summary>
        public uint InflatedSize { get; set; } // 0x08 - 0x0B (0x0F - 0x12)
    }
}
