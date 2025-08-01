namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Endianness of the file(?)
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wiseoverlay.h"/> 
    public enum Endianness : ushort
    {
        BigEndian = 0x0008,
        LittleEndian = 0x0800,
    }

    /// <summary>
    /// Opcodes for the state machine
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public enum OperationCode : byte
    {
        /// <summary>
        /// Custom deflate file header
        /// </summary>
        CustomDeflateFileHeader = 0x00,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x03 = 0x03,

        /// <summary>
        /// Form data?
        /// </summary>
        FormData = 0x04,

        /// <summary>
        /// .ini file, section-name and values for that section
        /// </summary>
        IniFile = 0x05,

        /// <summary>
        /// Deflated file just used by the installer? (No filename)
        /// </summary>
        UnknownDeflatedFile0x06 = 0x06,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x07 = 0x07,

        /// <summary>
        /// End branch
        /// </summary>
        EndBranch = 0x08,

        /// <summary>
        /// Function call ?
        /// </summary>
        FunctionCall = 0x09,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x0A = 0x0A,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x0B = 0x0B,

        /// <summary>
        /// if statement (new branch)
        /// </summary>
        IfStatement = 0x0C,

        /// <summary>
        /// else/default statement (inside if statement branch)
        /// </summary>
        ElseStatement = 0x0D,

        /// <summary>
        /// Start form data?
        /// </summary>
        StartFormData = 0x0F,

        /// <summary>
        /// End form data?
        /// </summary>
        EndFormData = 0x10,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x11 = 0x11,

        /// <summary>
        /// File on install medium (CD/DVD), to copy?
        /// </summary>
        FileOnInstallMedium = 0x12,

        /// <summary>
        /// Deflated file just used by the installer? (No filename)
        /// </summary>
        UnknownDeflatedFile0x14 = 0x14,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x15 = 0x15,

        /// <summary>
        /// Temp filename?
        /// </summary>
        TempFilename = 0x16,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x17 = 0x17,

        /// <summary>
        /// Skip this byte? On some installers also skip next 6 bytes FIXME
        /// </summary>
        Skip0x18 = 0x18,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x19 = 0x19,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x1A = 0x1A,

        /// <summary>
        /// Skip this byte
        /// </summary>
        Skip0x1B = 0x1B,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x1C = 0x1C,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x1D = 0x1D,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x1E = 0x1E,

        /// <summary>
        /// else if statement (inside if statement branch)
        /// </summary>
        ElseIfStatement = 0x23,

        /// <summary>
        /// Skip this byte? Only seen in RTCW
        /// </summary>
        Skip0x24 = 0x24,

        /// <summary>
        /// Skip this byte? Only seen in RTCW
        /// </summary>
        Skip0x25 = 0x25,

        /// <summary>
        /// Read 1 byte and 2 strings, only seen in cuteftp.exe, same
        /// as 0x15? or maybe even 0x23?
        /// </summary>
        ReadByteAndStrings = 0x30,
    }

    /// <summary>
    /// Wise installer overlay header flags
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wiseoverlay.h"/> 
    public enum OverlayHeaderFlags : uint
    {
        WISE_FLAG_UNKNOWN_1 = 0x00000001,

        WISE_FLAG_UNKNOWN_2 = 0x00000002,

        WISE_FLAG_UNKNOWN_3 = 0x00000004,

        WISE_FLAG_UNKNOWN_4 = 0x00000008,

        /// <remarks>
        /// Seen in hluplink.exe, Swat 3 and glsetup.exe
        /// </remarks>
        WISE_FLAG_UNKNOWN_5 = 0x00000010,

        WISE_FLAG_UNKNOWN_6 = 0x00000020,

        WISE_FLAG_UNKNOWN_7 = 0x00000040,

        WISE_FLAG_UNKNOWN_8 = 0x00000080,

        WISE_FLAG_PK_ZIP = 0x00000100,

        WISE_FLAG_UNKNOWN_10 = 0x00000200,

        WISE_FLAG_UNKNOWN_11 = 0x00000400,

        WISE_FLAG_UNKNOWN_12 = 0x00000800,

        WISE_FLAG_UNKNOWN_13 = 0x00001000,

        WISE_FLAG_UNKNOWN_14 = 0x00002000,

        WISE_FLAG_UNKNOWN_15 = 0x00004000,

        WISE_FLAG_UNKNOWN_16 = 0x00008000,

        WISE_FLAG_UNKNOWN_17 = 0x00010000,

        /// <remarks>
        /// Only seen in Swat 3
        /// </remarks>
        WISE_FLAG_UNKNOWN_18 = 0x00020000,

        WISE_FLAG_UNKNOWN_19 = 0x00040000,

        /// <remarks>
        /// Only seen set in Wild Wheels
        /// </remarks>
        WISE_FLAG_UNKNOWN_20 = 0x00080000,

        WISE_FLAG_UNKNOWN_21 = 0x00100000,

        WISE_FLAG_UNKNOWN_22 = 0x00200000,

        /// <remarks>
        /// Only seen in glsetup.exe
        /// </remarks>
        WISE_FLAG_UNKNOWN_23 = 0x00400000,

        WISE_FLAG_UNKNOWN_24 = 0x00800000,

        WISE_FLAG_UNKNOWN_25 = 0x01000000,

        WISE_FLAG_UNKNOWN_26 = 0x02000000,

        WISE_FLAG_UNKNOWN_27 = 0x04000000,

        WISE_FLAG_UNKNOWN_28 = 0x08000000,

        WISE_FLAG_UNKNOWN_29 = 0x10000000,

        WISE_FLAG_UNKNOWN_30 = 0x20000000,

        WISE_FLAG_UNKNOWN_31 = 0x40000000,

        WISE_FLAG_UNKNOWN_32 = 0x80000000,

    }
}
