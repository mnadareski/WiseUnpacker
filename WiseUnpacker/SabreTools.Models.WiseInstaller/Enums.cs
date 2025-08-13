namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// The CharacterSet Enumeration defines the possible sets of
    /// character glyphs that are defined in fonts for graphics output.
    /// </summary>
    /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/0d0b32ac-a836-4bd2-a112-b6000a1b4fc9"/> 
    public enum CharacterSet : uint
    {
        ANSI_CHARSET = 0x00000000,
        DEFAULT_CHARSET = 0x00000001,
        SYMBOL_CHARSET = 0x00000002,
        MAC_CHARSET = 0x0000004D,
        SHIFTJIS_CHARSET = 0x00000080,
        HANGUL_CHARSET = 0x00000081,
        JOHAB_CHARSET = 0x00000082,
        GB2312_CHARSET = 0x00000086,
        CHINESEBIG5_CHARSET = 0x00000088,
        GREEK_CHARSET = 0x000000A1,
        TURKISH_CHARSET = 0x000000A2,
        VIETNAMESE_CHARSET = 0x000000A3,
        HEBREW_CHARSET = 0x000000B1,
        ARABIC_CHARSET = 0x000000B2,
        BALTIC_CHARSET = 0x000000BA,
        RUSSIAN_CHARSET = 0x000000CC,
        THAI_CHARSET = 0x000000DE,
        EASTEUROPE_CHARSET = 0x000000EE,
        OEM_CHARSET = 0x000000FF
    }

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
        /// Install a file
        /// </summary>
        InstallFile = 0x00,

        /// <summary>
        /// Invalid case
        /// </summary>
        Invalid0x01 = 0x01,

        /// <summary>
        /// No-op
        /// </summary>
        /// <remarks>Empty case in WISE0001.DLL</remarks>
        NoOp = 0x02,

        /// <summary>
        /// Display Message
        /// </summary>
        DisplayMessage = 0x03,

        /// <summary>
        /// User-Defined Action Step
        /// </summary>
        UserDefinedActionStep = 0x04,

        /// <summary>
        /// Edit INI File
        /// </summary>
        EditIniFile = 0x05,

        /// <summary>
        /// Display billboard
        /// </summary>
        DisplayBillboard = 0x06,

        /// <summary>
        /// Execute Program
        /// </summary>
        ExecuteProgram = 0x07,

        /// <summary>
        /// End block
        /// </summary>
        EndBlock = 0x08,

        /// <summary>
        /// Call DLL Function
        /// </summary>
        CallDllFunction = 0x09,

        /// <summary>
        /// Edit Registry
        /// </summary>
        EditRegistry = 0x0A,

        /// <summary>
        /// Delete File
        /// </summary>
        DeleteFile = 0x0B,

        /// <summary>
        /// If/While Statement
        /// </summary>
        IfWhileStatement = 0x0C,

        /// <summary>
        /// Else Statement
        /// </summary>
        ElseStatement = 0x0D,

        /// <summary>
        /// Invalid case
        /// </summary>
        Invalid0x0E = 0x0E,

        /// <summary>
        /// Start User-Defined Action
        /// </summary>
        StartUserDefinedAction = 0x0F,

        /// <summary>
        /// End User-Defined Action
        /// </summary>
        EndUserDefinedAction = 0x10,

        /// <summary>
        /// Create Directory
        /// </summary>
        CreateDirectory = 0x11,

        /// <summary>
        /// Copy Local File
        /// </summary>
        CopyLocalFile = 0x12,

        /// <summary>
        /// Invalid case
        /// </summary>
        Invalid0x13 = 0x13,

        /// <summary>
        /// Custom Dialog Set
        /// </summary>
        CustomDialogSet = 0x14,

        /// <summary>
        /// Get System Information
        /// </summary>
        GetSystemInformation = 0x15,

        /// <summary>
        /// Get Temporary Filename
        /// </summary>
        GetTemporaryFilename = 0x16,

        /// <summary>
        /// Play Multimedia File
        /// </summary>
        PlayMultimediaFile = 0x17,

        /// <summary>
        /// New Event
        /// </summary>
        /// <remarks>Returns 0xffffffff in WISE0001.DLL</remarks>
        NewEvent = 0x18,

        /// <summary>
        /// Install ODBC Driver
        /// </summary>
        /// <remarks>
        /// Available documentation does not mention this action,
        /// instead saying that the driver needs to be installed
        /// before configuration. This may be a holdover from
        /// older versions that required driver installation.
        /// </remarks>
        InstallODBCDriver = 0x19,

        /// <summary>
        /// Config ODBC Data Source
        /// </summary>
        ConfigODBCDataSource = 0x1A,

        /// <summary>
        /// Include Script(?)
        /// </summary>
        /// <remarks>
        /// Acts like a no-op in the parsed script. Includes a
        /// "Pathname" to the file to be included.
        /// 
        /// In WISE0001.DLL, it seeks forward until it doesn't
        /// find another 0x1B value again. Indicates that this
        /// may not be "Include Script" as previously expected.
        /// </remarks>
        IncludeScript = 0x1B,

        /// <summary>
        /// Add Text to INSTALL.LOG
        /// </summary>
        AddTextToInstallLog = 0x1C,

        /// <summary>
        /// Rename File/Directory
        /// </summary>
        RenameFileDirectory = 0x1D,

        /// <summary>
        /// Open/Close Install.log
        /// </summary>
        OpenCloseInstallLog = 0x1E,

        /// <summary>
        /// Invalid case
        /// </summary>
        Invalid0x1F = 0x1F,

        /// <summary>
        /// Invalid case
        /// </summary>
        Invalid0x20 = 0x20,

        /// <summary>
        /// Invalid case
        /// </summary>
        Invalid0x21 = 0x21,

        /// <summary>
        /// Invalid case
        /// </summary>
        Invalid0x22 = 0x22,

        /// <summary>
        /// ElseIf Statement
        /// </summary>
        ElseIfStatement = 0x23,

        /// <summary>
        /// Enable repair?
        /// </summary>
        /// <remarks>
        /// The flag used by this and <see cref="Unknown0x25"/> seems
        /// to only be referenced in contexts where there are registry
        /// keys read and written, specifically about repair. 
        /// </remarks>
        Unknown0x24 = 0x24,

        /// <summary>
        /// Disable repair?
        /// </summary>
        /// <remarks>
        /// The flag used by this and <see cref="Unknown0x24"/> seems
        /// to only be referenced in contexts where there are registry
        /// keys read and written, specifically about repair. 
        /// </remarks>
        Unknown0x25 = 0x25,

        // Check in WISE0001.DLL suggests that there could be up
        // to opcode 0x3F. If the opcode is greater than 0x3F,
        // then the installation aborts with 0xfffffffe.
        //
        // Opcode 0x3F returns a value of 0xffffffff.
    }

    /// <summary>
    /// Wise installer overlay header flags
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wiseoverlay.h"/> 
    public enum OverlayHeaderFlags : uint
    {
        /// <summary>
        /// This value is checked for explicitly. If this value
        /// and <see cref="WISE_FLAG_UNKNOWN_1"/> are both not
        /// set, then it shows the commandline when silent mode
        /// is disabled.
        /// 
        /// If just this value is set, the bottom of the window
        /// is adjusted by (MainWindowBottom * 3) / 4 and then
        /// displays the window with the current size.
        /// </summary>
        WISE_FLAG_UNKNOWN_0 = 0x00000001,

        /// <summary>
        /// This value is checked for explicitly. If this value
        /// and not <see cref="WISE_FLAG_UNKNOWN_0"/> is set
        /// and <see cref="WISE_FLAG_UNKNOWN_7"/> is set and
        /// the silent mode flag is not enabled, it runs a
        /// function. Maybe debug window?
        /// 
        /// If this value and <see cref="WISE_FLAG_UNKNOWN_0"/>
        /// are both not set, then it displays the window
        /// maximized.
        /// 
        /// If just this value is set and silent mode is enabled,
        /// the position of the window is set to full screen
        /// but the window is not shown.
        /// </summary>
        WISE_FLAG_UNKNOWN_1 = 0x00000002,

        WISE_FLAG_UNKNOWN_2 = 0x00000004,

        /// <summary>
        /// Enable fullscreen installer
        /// </summary>
        /// <remarks>
        /// If this flag is enabled, it sets the following window flags:
        /// WS_BORDER | WS_DLGFRAME | WS_SYSMENU | WS_MAXIMIZEBOX | WS_TILED
        /// 
        /// If this flag is disabled, it sets the following window flags:
        /// WS_POPUP | WS_SIZEBOX
        /// </remarks>
        WISE_FLAG_FULLSCREEN = 0x00000008,

        /// <remarks>
        /// Seen in hluplink.exe, Swat 3 and glsetup.exe
        /// </remarks>
        WISE_FLAG_UNKNOWN_4 = 0x00000010,

        WISE_FLAG_UNKNOWN_5 = 0x00000020,

        WISE_FLAG_UNKNOWN_6 = 0x00000040,

        /// <remarks>
        /// This value is checked for explicitly. If this value is
        /// set and the commandline doesn't specify silent mode
        /// and the first byte of the flags (f & 3 != 2), it runs
        /// a function. Maybe debug window?
        /// </remarks>
        WISE_FLAG_UNKNOWN_7 = 0x00000080,

        /// <summary>
        /// Indicates that PKZIP containers are used
        /// </summary>
        WISE_FLAG_PK_ZIP = 0x00000100,

        WISE_FLAG_UNKNOWN_9 = 0x00000200,

        WISE_FLAG_UNKNOWN_10 = 0x00000400,

        WISE_FLAG_UNKNOWN_11 = 0x00000800,

        /// <remarks>
        /// This value is checked for explicitly
        /// </remarks>
        WISE_FLAG_UNKNOWN_12 = 0x00001000,

        WISE_FLAG_UNKNOWN_13 = 0x00002000,

        WISE_FLAG_UNKNOWN_14 = 0x00004000,

        WISE_FLAG_UNKNOWN_15 = 0x00008000,

        WISE_FLAG_UNKNOWN_16 = 0x00010000,

        WISE_FLAG_UNKNOWN_17 = 0x00020000,

        WISE_FLAG_UNKNOWN_18 = 0x00040000,

        /// <remarks>
        /// Only seen set in Wild Wheels
        /// </remarks>
        WISE_FLAG_UNKNOWN_19 = 0x00080000,

        WISE_FLAG_UNKNOWN_20 = 0x00100000,

        WISE_FLAG_UNKNOWN_21 = 0x00200000,

        /// <remarks>
        /// Only seen in glsetup.exe
        /// </remarks>
        WISE_FLAG_UNKNOWN_22 = 0x00400000,

        WISE_FLAG_UNKNOWN_23 = 0x00800000,

        WISE_FLAG_UNKNOWN_24 = 0x01000000,

        WISE_FLAG_UNKNOWN_25 = 0x02000000,

        WISE_FLAG_UNKNOWN_26 = 0x04000000,

        WISE_FLAG_UNKNOWN_27 = 0x08000000,

        WISE_FLAG_UNKNOWN_28 = 0x10000000,

        WISE_FLAG_UNKNOWN_29 = 0x20000000,

        /// <summary>
        /// If enabled, sets the same flag as /M4 commandline
        /// </summary>
        /// <remarks>
        /// The /M4 commandline parameter, this flag, and then
        /// some value (DAT_00404270) being 0 all lead to the
        /// same outcome. The set of installers that include
        /// this flag need to be further analyzed to see what
        /// possible files are omitted if this flag is set.
        /// 
        /// Preliminary inspection of output files does not
        /// show any notable missing files. It is very possible
        /// that this represents a file that is not currently
        /// extracted.
        /// </remarks>
        WISE_FLAG_FORCE_M4 = 0x40000000,

        WISE_FLAG_UNKNOWN_31 = 0x80000000,

    }
}
