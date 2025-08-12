namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Header for the Wise script
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptHeader
    {
        /// <summary>
        /// Flags, unknown mapping
        /// </summary>
        /// <remarks>
        /// The high byte (0x01) being any value but 0x00 indicates
        /// that a 32-bit library will be used.
        /// </remarks>
        public ushort Flags { get; set; } // 0x00 - 0x01

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>
        /// Read in a loop with <see cref="UnknownU16_2"/>, possibly
        /// an offset? It's read into an array.
        /// 
        /// Both values are then used to build variable names if they're
        /// non-zero. The variable names use "SYS" as the template.
        /// The values are then seemingly read over?
        /// </remarks>
        public ushort UnknownU16_1 { get; set; } // 0x02 - 0x03

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>
        /// Read in a loop with <see cref="UnknownU16_1"/>, possibly
        /// an offset? It's read into an array.
        /// 
        /// Both values are then used to build variable names if they're
        /// non-zero. The variable names use "SYS" as the template.
        /// The values are then seemingly read over?
        /// </remarks>
        public ushort UnknownU16_2 { get; set; } // 0x04 - 0x05

        /// <summary>
        /// Total deflated size of OP 0x00 files?
        /// </summary>
        /// <remarks>
        /// This seems to match the offset we can do filesize - SomeOffset1
        /// to get to the script file deflate offset, but not on all
        /// installers..
        /// 
        /// Values from WISE0001.DLL
        /// - < 0   - Display abort installation message and return 1?
        /// - 0x400 - Breaks a loop?
        /// - 0x800 - Returns 0?
        /// </remarks>
        public uint SomeOffset1 { get; set; } // 0x05 - 0x08

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>
        /// Used as a size to allocate memory in WISE0001.DLL
        /// </remarks>
        public uint SomeOffset2 { get; set; } // 0x09 - 0x0C

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>
        /// 4 bytes
        /// 
        /// In WISE0001.DLL, the first byte of this array is checked
        /// to be 0x00. If it's not 0x00, then it skips a string, ending
        /// at the next null terminator. The string at that offset is
        /// then comapred to ...
        /// </remarks>
        public byte[]? UnknownBytes_2 { get; set; } // 0x0D - 0x10

        /// <summary>
        /// Creation of this WiseScript.bin since UNIX epoch
        /// </summary>
        public uint DateTime { get; set; } // 0x11 - 0x14

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>
        /// 22 bytes
        /// 
        /// byte[0]
        ///     0x00 - ????
        ///     0x40 - ????
        /// byte[1]
        ///     0x00 - ????
        ///     0x10 - ????
        public byte[]? Unknown_22 { get; set; } // 0x15 - 0x2B

        /// <summary>
        /// FTP URL for online downloading
        /// </summary>
        public string? FTPURL { get; set; } // 0x2C -

        /// <summary>
        /// Log pathname
        /// </summary>
        public string? LogPathname { get; set; }

        /// <summary>
        /// Message font
        /// </summary>
        public string? MessageFont { get; set; }

        /// <summary>
        /// Font size for message fonts
        /// </summary>
        public uint FontSize { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>2 bytes</remarks>
        public byte[]? Unknown_2 { get; set; }

        /// <summary>
        /// if languageCount > 1: the total string count is larger, there
        /// will be the language selection strings at top and the normal
        /// strings (56) minus 1 (55) will be times the languageCount, plus 2.
        /// </summary>
        /// <remarks>
        /// Language selection strings example when there are 6 languages:
        ///
        ///   "Select Language"                  ;; selection string 1
        ///   "Please Select a Language"         ;; selection string 2
        ///   "U.S. English"                     ;; language name
        ///   "ENU"                              ;; language short
        ///   "Fran.ias"
        ///   "FRA"
        ///   "Deutsch"
        ///   "DEU"
        ///   "Portugu.s"
        ///   "PTG"
        ///   "Espa.ol"
        ///   "ESN"
        ///   "Italiano"
        ///   "ITA"
        ///
        /// The total string count seen with 6 languages is 434 and the
        /// total string count seen with 1 language has been always 56, for a
        /// languageCount of 5 the string count should be 287. As seen for now.
        ///
        /// if (languageCount > 1) {
        ///   stringCount = (55 * languageCount) + (languageCount * 2) + 2;
        /// }
        /// else {
        ///   stringCount = 56 = (55 * languageCount) + languageCount
        /// }
        ///
        /// The container size (uint8_t) is a guess, because the neightbour
        /// bytes are almost all 0x00 (as seen for now). So did you find a
        /// installer with more then 255 languages? then FIXME :')
        /// </remarks>
        public byte LanguageCount { get; set; }

        /// <summary>
        /// Unknown strings, most set to \0, only seen set in smartsd.exe
        /// </summary>
        /// <remarks>7 strings</remarks>
        public string[]? UnknownStrings_7 { get; set; }

        /// <summary>
        /// 1 string when <see cref="LanguageCount"/> is 1,
        /// otherwise (<see cref="LanguageCount"/> * 2) + 2
        /// </summary>
        public string[]? LanguageSelectionStrings { get; set; }

        /// <summary>
        /// 55 * <see cref="LanguageCount"/> strings
        /// </summary>
        /// <remarks>In trimmed scripts, this number seems to be 46?</remarks>
        public string[]? ScriptStrings { get; set; }
    }
}
