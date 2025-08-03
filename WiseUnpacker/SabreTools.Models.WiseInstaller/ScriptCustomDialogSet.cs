namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Compressed custom dialog set data
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// TODO: Document the decompressed file format
    public class ScriptCustomDialogSet : MachineStateData
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

        /// <summary>
        /// Display variable name
        /// </summary>
        public string? DisplayVariable { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string? Name { get; set; }
    }
}
