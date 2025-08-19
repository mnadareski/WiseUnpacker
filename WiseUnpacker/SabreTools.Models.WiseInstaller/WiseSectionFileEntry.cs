namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// File entry
    /// </summary>
    public class WiseSectionFileEntry
    {
        /// <summary>
        /// The file data. Size known from header values.
        /// </summary>
        public byte[]? File { get; set; }
        
        /// <summary>
        /// CRC32
        /// </summary>
        public byte[]? Crc32 { get; set; }
    }
}