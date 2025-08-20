namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// File entry
    /// </summary>
    public class FileEntry
    {
        /// <summary>
        /// The file data. Size known from header values.
        /// </summary>
        public byte[]? File { get; set; }

        /// <summary>
        /// CRC-32
        /// </summary>
        public uint Crc32 { get; set; }
    }
}
