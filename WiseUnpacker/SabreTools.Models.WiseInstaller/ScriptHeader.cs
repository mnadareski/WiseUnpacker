namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Header for the Wise script
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptHeader
    {
        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>5 bytes</remarks>
        public byte[]? Unknown_5 { get; set; }

        /// <summary>
        /// Total deflated size of OP 0x00 files?
        /// </summary>
        /// <remarks>
        /// This seems to match the offset we can do filesize - SomeOffset1
        /// to get to the script file deflate offset, but not on all
        /// installers..
        /// </remarks>
        public uint SomeOffset1 { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public uint SomeOffset2 { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>4 bytes</remarks>
        public byte[]? Unknown_4 { get; set; }

        /// <summary>
        /// Creation of this WiseScript.bin since UNIX epoch
        /// </summary>
        public uint DateTime { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>22 bytes</remarks>
        public byte[]? Unknown_22 { get; set; }

        /// <summary>
        /// Only seen in glsetup.exe, others just \0
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// \0 terminated string
        /// </summary>
        public string? LogPath { get; set; }

        /// <summary>
        /// \0 terminated string
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
