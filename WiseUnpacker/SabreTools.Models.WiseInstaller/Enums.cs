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
        /// Install a file
        /// </summary>
        InstallFile = 0x00,

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
        /// Deflated file just used by the installer? (No filename)
        /// </summary>
        UnknownDeflatedFile0x06 = 0x06,

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
        /// Start User-Defined Action
        /// </summary>
        StartUserDefinedAction = 0x0F,

        /// <summary>
        /// End User-Defined Action
        /// </summary>
        EndUserDefinedAction = 0x10,

        /// <summary>
        /// Ignore Output Files(?)
        /// </summary>
        /// <remarks>Unconfirmed</remarks>
        IgnoreOutputFiles = 0x11,

        /// <summary>
        /// Copy Local File
        /// </summary>
        CopyLocalFile = 0x12,

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
        NewEvent = 0x18,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0x19 = 0x19,

        /// <summary>
        /// Config ODBC Data Source
        /// </summary>
        ConfigODBCDataSource = 0x1A,

        /// <summary>
        /// Include Script
        /// </summary>
        /// <remarks>
        /// Acts like a no-op in the parsed script. Includes a
        /// "Pathname" to the file to be included.
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
        /// ElseIf Statement
        /// </summary>
        ElseIfStatement = 0x23,
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

        /// <summary>
        /// Indicates that PKZIP containers are used
        /// </summary>
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
        /// May indicate either install script or a valid Ocxreg32.EXE
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

        /// <remarks>
        /// May indicate either install script or a valid Ocxreg32.EXE
        /// </remarks>
        WISE_FLAG_UNKNOWN_32 = 0x80000000,

    }
}
