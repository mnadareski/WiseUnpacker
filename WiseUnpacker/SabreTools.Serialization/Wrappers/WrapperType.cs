namespace SabreTools.Serialization.Wrappers
{
    /// <summary>
    /// Represents each of the IWrapper implementations
    /// </summary>
    public enum WrapperType
    {
        /// <summary>
        /// Unknown or unsupported
        /// </summary>
        UNKNOWN,

        /// <summary>
        /// AACS media key block
        /// </summary>
        AACSMediaKeyBlock,

        /// <summary>
        /// BD+ SVM
        /// </summary>
        BDPlusSVM,

        /// <summary>
        /// BFPK custom archive
        /// </summary>
        BFPK,

        /// <summary>
        /// Half-Life Level
        /// </summary>
        BSP,

        /// <summary>
        /// bzip2 archive
        /// </summary>
        BZip2,

        /// <summary>
        /// Compound File Binary
        /// </summary>
        CFB,

        /// <summary>
        /// MAME Compressed Hunks of Data
        /// </summary>
        CHD,

        /// <summary>
        /// CTR Importable Archive
        /// </summary>
        CIA,

        /// <summary>
        /// Executable or library
        /// </summary>
        /// <remarks>Includes MZ, NE, LE/LX, and PE</remarks>
        Executable,

        /// <summary>
        /// Half-Life Game Cache File
        /// </summary>
        GCF,

        /// <summary>
        /// gzip archive
        /// </summary>
        GZip,

        /// <summary>
        /// Key-value pair INI file
        /// </summary>
        /// <remarks>Currently has no IWrapper implementation</remarks>
        IniFile,

        /// <summary>
        /// InstallShield archive v3
        /// </summary>
        InstallShieldArchiveV3,

        /// <summary>
        /// InstallShield cabinet file
        /// </summary>
        InstallShieldCAB,

        /// <summary>
        /// Link Data Security encrypted file
        /// </summary>
        /// <remarks>Currently has no IWrapper implementation</remarks>
        LDSCRYPT,

        /// <summary>
        /// LZ-compressed file, KWAJ variant
        /// </summary>
        LZKWAJ,

        /// <summary>
        /// LZ-compressed file, QBasic variant
        /// </summary>
        LZQBasic,

        /// <summary>
        /// LZ-compressed file, SZDD variant
        /// </summary>
        LZSZDD,

        /// <summary>
        /// Microsoft cabinet file
        /// </summary>
        MicrosoftCAB,

        /// <summary>
        /// MPQ game data archive
        /// </summary>
        MoPaQ,

        /// <summary>
        /// Nintendo 3DS cart image
        /// </summary>
        N3DS,

        /// <summary>
        /// Half-Life No Cache File
        /// </summary>
        NCF,

        /// <summary>
        /// Nintendo DS/DSi cart image
        /// </summary>
        Nitro,

        /// <summary>
        /// Half-Life Package File
        /// </summary>
        PAK,

        /// <summary>
        /// NovaLogic Game Archive Format
        /// </summary>
        PFF,

        /// <summary>
        /// PIC data object
        /// </summary>
        /// <remarks>Currently has no detection method</remarks>
        PIC,

        /// <summary>
        /// PKWARE ZIP archive and derivatives
        /// </summary>
        PKZIP,

        /// <summary>
        /// PlayJ audio file
        /// </summary>
        PlayJAudioFile,

        /// <summary>
        /// PlayJ playlist file
        /// </summary>
        /// <remarks>Currently has no detection method/remarks>
        PlayJPlaylist,

        /// <summary>
        /// Quantum archive
        /// </summary>
        Quantum,

        /// <summary>
        /// RAR archive
        /// </summary>
        RAR,

        /// <summary>
        /// RealArcade Installer
        /// </summary>
        /// <remarks>Currently has no IWrapper implementation</remarks>
        RealArcadeInstaller,

        /// <summary>
        /// RealArcade Mezzanine
        /// </summary>
        /// <remarks>Currently has no IWrapper implementation</remarks>
        RealArcadeMezzanine,

        /// <summary>
        /// SecuROM DFA File
        /// </summary>
        SecuROMDFA,

        /// <summary>
        /// 7-zip archive
        /// </summary>
        SevenZip,

        /// <summary>
        /// StarForce FileSystem file
        /// </summary>
        /// <remarks>Currently has no IWrapper implementation</remarks>
        SFFS,

        /// <summary>
        /// SGA
        /// </summary>
        SGA,

        /// <summary>
        /// Tape archive
        /// </summary>
        TapeArchive,

        /// <summary>
        /// Various generic textfile formats
        /// </summary>
        /// <remarks>Currently has no IWrapper implementation</remarks>
        Textfile,

        /// <summary>
        /// Half-Life 2 Level
        /// </summary>
        VBSP,

        /// <summary>
        /// Valve Package File
        /// </summary>
        VPK,

        /// <summary>
        /// Half-Life Texture Package File
        /// </summary>
        WAD,

        /// <summary>
        /// Wise Installer Overlay Header
        /// </summary>
        WiseOverlayHeader,

        /// <summary>
        /// Wise Installer Script File
        /// </summary>
        WiseScript,

        /// <summary>
        /// xz archive
        /// </summary>
        XZ,

        /// <summary>
        /// Xbox Package File
        /// </summary>
        XZP,
    }
}
