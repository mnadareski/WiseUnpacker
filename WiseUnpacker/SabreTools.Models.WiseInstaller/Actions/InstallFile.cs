namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Install File
    /// 
    /// This action installs files on the destination computer. Each file or directory to be installed
    /// must have a separate Install File(s) action.
    /// </summary>
    /// <remarks>
    /// Multiple files can be included in the installer from
    /// a single source Install File statement in the script.
    /// Wildcards have been observed in a few examples to denote
    /// entire directories or subdirectories being copied.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class InstallFile : MachineStateData
    {
        /// <summary>
        /// Values of 0x8000, 0x8100, 0x0000, 0x9800 0xA100 have been observed
        /// </summary>
        /// <remarks>
        /// Expected flags:
        /// - Include Sub-Directories (unknown)
        /// - Shared DLL Counter (unknown)
        /// - No Progress Bar (unknown)
        /// - Self-Register OCX/DLL/EXE/TLB (unknown)
        /// - Replace Existing File [Always, Never, Check File [Doesn't Matter, Same or Older, Older]] (unknown)
        /// - Retain Duplicates in Path (unknown)
        /// 
        /// This is two separate fields in WISE0001.DLL (bVar1 and bVar2)
        /// 
        /// if (bVar2 & 0x48) == 0 && (bVar1 & 0x40) == 0
        ///     Checksum set? Verify the file if possible
        ///     if (bVar1 & 1) != 0 && action[0x13] != 0
        ///         See Operand_7
        /// </remarks>
        public ushort Flags { get; set; } // 0x01 - 0x02

        /// <summary>
        /// Start of the deflated data
        /// </summary>
        public uint DeflateStart { get; set; } // 0x03 - 0x06

        /// <summary>
        /// End of the deflated data
        /// </summary>
        public uint DeflateEnd { get; set; } // 0x07 - 0x0A

        /// <summary>
        /// MS-DOS date
        /// </summary>
        public ushort Date { get; set; } // 0x0B - 0x0C

        /// <summary>
        /// MS-DOS time
        /// </summary>
        public ushort Time { get; set; } // 0x0D - 0x0E

        /// <summary>
        /// Inflated data size
        /// </summary>
        public uint InflatedSize { get; set; } // 0x0F - 0x12

        /// <summary>
        /// Unknown, 20 * \0? Not seen in hl15of16.exe and hl1316.exe
        /// </summary>
        /// <remarks>
        /// 20 bytes
        /// 0x13 (4 bytes) - if (bVar1 & 1) != 0 && [0x13] != 0 [BLOCK]
        /// 0x13 (4 bytes) = local_70
        /// 0x17 (4 bytes) - local_6c
        /// 0x1B (4 bytes) - local_68.PrivilegeCount
        /// 0x1F (4 bytes) - local_68.Control
        /// 0x23 (4 bytes) - local_68.Privilege[0].Luid.LowPart
        /// </remarks>
        public byte[]? Operand_7 { get; set; } // 0x13 - 0x26

        /// <summary>
        /// CRC-32 checksum of the data
        /// </summary>
        /// <remarks>Do not check when it is 0</remarks>
        public uint Crc32 { get; set; } // 0x27 - 0x2A

        /// <summary>
        /// Destination pathname
        /// </summary>
        public string? DestinationPathname { get; set; } // 0x2B - ?

        /// <summary>
        /// One file text per language
        /// </summary>
        public string[]? Description { get; set; }

        /// <summary>
        /// Seen used on hl15of16.exe and hl1316.exe, on others its \0
        /// </summary>
        public string? Operand_11 { get; set; }
    }
}
