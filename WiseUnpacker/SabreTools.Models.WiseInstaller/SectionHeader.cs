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
    public class SectionHeader
    {
        /// <summary>
        /// Unknown Data Size.
        /// </summary>
        public uint UnknownDataSize { get; set; }

        /// <summary>
        /// Value 1, (possibly?) the size of the second file entry. This has thus far always been observed
        /// with non-0x00 values for values 3 and 4, and usually, but not always, value 5 as well. Thus far always been
        /// observed to be an executable, filename unknown. Likely assists with extraction. A few samples with a
        /// non-0x00 value 1 seem to have value 5 as 0x00, meaning that it being observed as the second file may just be
        /// coincidence, and values 1 and 5 may refer to files with a specific purpose/meaning.
        /// </summary>
        public uint SecondExecutableFileEntryLength { get; set; }

        /// <summary>
        /// Unknown value 2. Currently unobserved in any samples.
        /// </summary>
        public uint UnknownValue2 { get; set; }

        /// <summary>
        /// Unknown value 3. This has thus far always been observed with a non-0x00 value for value 4.
        /// </summary>
        public uint UnknownValue3 { get; set; }

        /// <summary>
        /// Unknown value 4. This has thus far always been observed with a non-0x00 value for value 3.
        /// </summary>
        public uint UnknownValue4 { get; set; }

        /// <summary>
        /// Value 5, the size of the first entry. This has thus far always been observed with
        /// non-0x00 values for values 3 and 4, and guarantees the presence of one file before the main msi installer
        /// file. Thus far always been observed to be an executable, filename unknown. Likely assists with extraction.
        /// </summary>
        public uint FirstExecutableFileEntryLength { get; set; }

        /// <summary>
        /// Value 6, the size of the "main" msi installer file entry. Always at the end of the file entries.
        /// </summary>
        public uint MsiFileEntryLength { get; set; }

        /// <summary>
        /// Unknown value 7. Currently unobserved in any samples.
        /// </summary>
        public uint UnknownValue7 { get; set; }

        /// <summary>
        /// Unknown value 8. Currently unobserved in any samples.
        /// </summary>
        public uint UnknownValue8 { get; set; }

        /// <summary>
        /// Value 9, the size of the third entry.
        /// </summary>
        public uint ThirdExecutableFileEntryLength { get; set; }

        /// <summary>
        /// Unknown value 10.
        /// </summary>
        public uint UnknownValue10 { get; set; }

        /// <summary>
        /// Unknown value 11. Currently unobserved in any samples.
        /// </summary>
        public uint UnknownValue11 { get; set; }

        /// <summary>
        /// Unknown value 12. Currently unobserved in any samples.
        /// </summary>
        public uint UnknownValue12 { get; set; }

        /// <summary>
        /// Unknown value 13. Currently unobserved in any samples.
        /// </summary>
        public uint UnknownValue13 { get; set; }

        /// <summary>
        /// Unknown value 14. Currently unobserved in any samples.
        /// </summary>
        public uint UnknownValue14 { get; set; }

        /// <summary>
        /// Unknown value 15. Currently unobserved in any samples.
        /// </summary>
        public uint UnknownValue15 { get; set; }

        /// <summary>
        /// Unknown value 16.
        /// </summary>
        public uint UnknownValue16 { get; set; }

        /// <summary>
        /// Unknown value 17.
        /// </summary>
        public uint UnknownValue17 { get; set; }

        /// <summary>
        /// Unknown value 18. Currently unobserved in any samples.
        /// </summary>
        public uint UnknownValue18 { get; set; }

        /// <summary>
        /// Byte array representing version. Byte array used due to unknown size and type for version.
        /// </summary>
        public byte[]? Version { get; set; }
        
        /// <summary>
        /// String representing the WIS[etc].TMP string
        /// </summary>
        public string? TmpString { get; set; }
        
        /// <summary>
        /// String representing the GUID string.
        /// </summary>
        public string? GuidString { get; set; }
        
        /// <summary>
        /// String representing a version number. This isn't the version of the .WISE installer itself, as it is
        /// entirely inconsistent even within the same week. Likely refers to a version for what's being installed
        /// rather than the installer itself
        /// </summary>
        public string? NonWiseVersion { get; set; }

        /// <summary>
        /// Unknown. May also refer to a non-value for pre-78-offset executables and only a value for 78-offset-onwards
        /// ones.
        /// </summary>
        public byte[]? PreFontValue {get; set;}
        
        /// <summary>
        /// Font size
        /// </summary>
        public int FontSize { get; set; }
        
        /// <summary>
        /// Byte array representing string lengths and info. Individual strings not predefined since number of strings
        /// will likely vary between many installers.
        /// </summary>
        public byte[]? PreStringValues { get; set; }

        /// <summary>
        /// Strings for the section. Size and any breakup of strings currently unknown.
        /// </summary>
        public byte[][]? Strings { get; set; }
    }
}
