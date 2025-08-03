namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Information about a file to be installed
    /// </summary>
    /// <remarks>
    /// Multiple files can be included in the installer from
    /// a single source Install File statement in the script.
    /// Wildcards have been observed in a few examples to denote
    /// entire directories or subdirectories being copied.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptFileHeader : MachineStateData
    {
        /// <summary>
        /// Values of 0x8000, 0x8100, 0x0000, 0x9800 0xA100 have been observed
        /// </summary>
        public ushort Operand_1 { get; set; }

        /// <summary>
        /// Start of the deflated data
        /// </summary>
        public uint DeflateStart { get; set; }

        /// <summary>
        /// End of the deflated data
        /// </summary>
        public uint DeflateEnd { get; set; }

        /// <summary>
        /// MS-DOS date
        /// </summary>
        public ushort Date { get; set; }

        /// <summary>
        /// MS-DOS time
        /// </summary>
        public ushort Time { get; set; }

        /// <summary>
        /// Inflated data size
        /// </summary>
        public uint InflatedSize { get; set; }

        /// <summary>
        /// Unknown, 20 * \0? Not seen in hl15of16.exe and hl1316.exe
        /// </summary>
        /// <remarks>20 bytes</remarks>
        public byte[]? Operand_7 { get; set; }

        /// <summary>
        /// CRC-32 checksum of the data
        /// </summary>
        /// <remarks>Do not check when it is 0</remarks>
        public uint Crc32 { get; set; }

        /// <summary>
        /// \0 terminated string
        /// </summary>
        public string? DestFile { get; set; }

        /// <summary>
        /// One file text per language, \0 terminated
        /// </summary>
        public string[]? FileTexts { get; set; }

        /// <summary>
        /// Seen used on hl15of16.exe and hl1316.exe, on others its \0
        /// </summary>
        public string? Operand_11 { get; set; }
    }
}
