namespace SabreTools.IO.Compression.Deflate
{
    /// <summary>
    /// Represents information about a DEFLATE stream
    /// </summary>
    public class DeflateInfo
    {
        /// <summary>
        /// Size of the deflated data
        /// </summary>
        /// <remarks>Set to a value less than 0 to ignore</remarks>
        public long InputSize { get; set; }

        /// <summary>
        /// Size of the inflated data
        /// </summary>
        /// <remarks>Set to a value less than 0 to ignore</remarks>
        public long OutputSize { get; set; }

        /// <summary>
        /// CRC-32 of the inflated data
        /// </summary>
        /// <remarks>Set to a value of 0 to ignore</remarks>
        public uint Crc32 { get; set; }
    }
}