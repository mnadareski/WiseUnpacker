using System.Collections.Generic;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.Matching;
using SabreTools.Models.WiseInstaller;
using static SabreTools.Models.WiseInstaller.Constants;

namespace SabreTools.Serialization.Deserializers
{
    public class WiseSection : BaseBinaryDeserializer<Section>
    {
        /// <inheritdoc/>
        public override Section? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            // Cache the current offset
            try
            {
                // Cache the current offset
                long initialOffset = data.Position;

                var section = new Section();

                #region Header

                var wiseSectionHeader = ParseWiseSectionHeader(data, initialOffset);

                // Checks if version was able to be read
                if (wiseSectionHeader?.Version == null)
                    return null;

                // Main MSI file
                if (wiseSectionHeader.MsiFileEntryLength == 0)
                    return null;
                else if (wiseSectionHeader.MsiFileEntryLength >= data.Length)
                    return null;

                // First executable file
                if (wiseSectionHeader.FirstExecutableFileEntryLength == 0)
                    return null;
                else if (wiseSectionHeader.FirstExecutableFileEntryLength >= data.Length)
                    return null;

                // Second executable file
                if (wiseSectionHeader.SecondExecutableFileEntryLength == 0)
                    return null;
                else if (wiseSectionHeader.SecondExecutableFileEntryLength >= data.Length)
                    return null;

                section.Header = wiseSectionHeader;

                #endregion

                #region Strings

                // TODO: Parse strings
                section.Strings = null;

                #endregion

                #region Entries

                var entries = new List<FileEntry>();

                // TODO: Parse file entries

                section.Entries = [.. entries];

                #endregion

                return section;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into a WiseSectionHeader
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="initialOffset">Initial offset to use in address comparisons</param>
        /// <returns>Filled WiseSectionHeader on success, null on error</returns>
        private static SectionHeader? ParseWiseSectionHeader(Stream data, long initialOffset)
        {
            var header = new SectionHeader();

            // Find offset of "WIS", determine header length, read presumed version value
            int headerLength = -1;
            foreach (int offset in WisOffsets)
            {
                data.Seek(initialOffset + offset, 0);
                byte[] checkBytes = data.ReadBytes(3);
                if (!checkBytes.EqualsExactly(WisString))
                    continue;

                headerLength = WiseSectionHeaderLengthDictionary[offset];
                int versionOffset = WiseSectionVersionOffsetDictionary[offset];

                data.Seek(initialOffset + offset - versionOffset, 0);
                header.Version = data.ReadBytes(versionOffset);
            }

            // If the header length couldn't be determined
            if (headerLength < 0)
                return null;

            //Seek back to the beginning of the section
            data.Seek(initialOffset, 0);

            header.UnknownValue0 = data.ReadUInt32LittleEndian();
            header.SecondExecutableFileEntryLength = data.ReadUInt32LittleEndian();
            header.UnknownValue2 = data.ReadUInt32LittleEndian();
            header.UnknownValue3 = data.ReadUInt32LittleEndian();
            header.UnknownValue4 = data.ReadUInt32LittleEndian();
            header.FirstExecutableFileEntryLength = data.ReadUInt32LittleEndian();
            header.MsiFileEntryLength = data.ReadUInt32LittleEndian();
            if (headerLength == 6)
                return header;

            header.UnknownValue7 = data.ReadUInt32LittleEndian();
            header.UnknownValue8 = data.ReadUInt32LittleEndian();
            if (headerLength == 8)
                return header;

            header.UnknownValue9 = data.ReadUInt32LittleEndian();
            header.UnknownValue10 = data.ReadUInt32LittleEndian();
            header.UnknownValue11 = data.ReadUInt32LittleEndian();
            header.UnknownValue12 = data.ReadUInt32LittleEndian();
            header.UnknownValue13 = data.ReadUInt32LittleEndian();
            header.UnknownValue14 = data.ReadUInt32LittleEndian();
            header.UnknownValue15 = data.ReadUInt32LittleEndian();
            header.UnknownValue16 = data.ReadUInt32LittleEndian();
            header.UnknownValue17 = data.ReadUInt32LittleEndian();
            if (headerLength == 17)
                return header;

            header.UnknownValue18 = data.ReadUInt32LittleEndian();
            if (headerLength == 18)
                return header;

            return header;
        }
    }
}
