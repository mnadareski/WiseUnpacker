namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Represents deflated file parameters
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class DeflateEntry
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
