namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// .WISE sections seem to be present in what virustotal calls "Self Extracting Wise Installer"s. These sections
    /// contain a header with anywhere from seven to nineteen 4-byte little-endian values, followed by what's possibly
    /// a version number, then string data, and then the file entries.
    /// 
    /// At some point in the header, there will be the characters "WIS" prepended by what seems to be a version number.
    /// This possible version number will be referred to from here on as just "version number", as it would be overly
    /// verbose to continue using "what is possibly a version number".
    /// 
    /// The WIS characters do not always align with a specific offset if the header is broken up by 4-byte blocks, and
    /// the version number seems to maintain this offset. It is currently unknown if the version number is 4 bytes long
    /// or five bytes long, it seems to vary.
    /// 
    /// Offsets of the "W" in the "WIS" string currently observed are 32, 33, 41, 77, 78, and 82. This seems to point to
    /// 4-6 known header lengths, depending on the deal with the version number/WIS offset.
    /// 
    /// Before the version number, there are anywhere from seven to nineteen 4-byte little-endian values depending on
    /// the length of the pre-string part of the header. These values will be described indexed from zero (0-18). The
    /// only one of these values that's ever guaranteed not to be all 0x00 is value 6. This is the size of the "main"
    /// file in the file entry part of the section, which is the size of the file, plus 4 bytes for its following crc32.
    /// This file seems to always be an msi installer that's extracted to the TEMP directory and ran to perform the
    /// actual install. This file has been observed to be nameless (a randomly generated alphanumeric string
    /// followed by .msi, with some examples including 2fbcb.msi and ddec.msi, serve as the filename. Name is random
    /// even when re-running the same installer) thus far. In many installers, all other pre-version values are 0x00,
    /// so this is the only guaranteed value.
    /// 
    /// All values not explicitly mentioned to never have been observed have been observed in at least one installer
    /// thus far.
    /// </summary>
    public class Section
    {
        /// <summary>
        /// Section header
        /// </summary>
        public SectionHeader? Header { get; set; }

        /// <summary>
        /// Strings for the section. Size and any breakup of strings currently unknown.
        /// </summary>
        public string? Strings { get; set; }

        /// <summary>
        /// At least one entry
        /// </summary>
        public FileEntry[]? Entries { get; set; }
    }
}
