using System.Collections.Generic;
using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.Matching;
using SabreTools.Models.WiseInstaller;
using static SabreTools.Models.WiseInstaller.Constants;

namespace SabreTools.Serialization.Deserializers
{
    public class WiseSectionHeader : BaseBinaryDeserializer<SectionHeader>
    {
        /// <inheritdoc/>
        public override SectionHeader? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long initialOffset = data.Position;

                var header = ParseSectionHeader(data, initialOffset);
                if (header == null)
                    return null;

                // Main MSI file
                // If these go wrong, then there actually is a major issue, and the fallback won't work.
                if (header.MsiFileEntryLength == 0)
                    return null;
                else if (header.MsiFileEntryLength >= data.Length)
                    return null;

                // First executable file
                if (header.FirstExecutableFileEntryLength >= data.Length)
                    return header;

                // Second executable file
                if (header.SecondExecutableFileEntryLength >= data.Length)
                    return header;

                return header;
            }
            catch
            {
                // Could header somehow be returned here too?
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into a WiseSectionHeader
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="initialOffset">Initial offset to use in address comparisons</param>
        /// <returns>Filled WiseSectionHeader on success, null on error</returns>
        public static SectionHeader? ParseSectionHeader(Stream data, long initialOffset)
        {
            var obj = new SectionHeader();

            // Setup required variables
            int wisOffset = -1;
            int headerLength = -1;

            // Find offset of "WIS", determine header length, read presumed version value
            foreach (int offset in WisOffsets)
            {
                data.Seek(initialOffset + offset, 0);
                byte[] checkBytes = data.ReadBytes(3);
                if (!checkBytes.EqualsExactly(WisString))
                    continue;

                headerLength = WiseSectionHeaderLengthDictionary[offset];
                int versionOffset = WiseSectionVersionOffsetDictionary[offset];

                data.Seek(initialOffset + offset - versionOffset, 0);
                obj.Version = data.ReadBytes(versionOffset);
                wisOffset = offset;
                break;
            }

            //Seek back to the beginning of the section
            data.Seek(initialOffset, 0);

            // Read common values
            obj.UnknownDataSize = data.ReadUInt32LittleEndian();
            obj.SecondExecutableFileEntryLength = data.ReadUInt32LittleEndian();
            obj.UnknownValue2 = data.ReadUInt32LittleEndian();
            obj.UnknownValue3 = data.ReadUInt32LittleEndian();
            obj.UnknownValue4 = data.ReadUInt32LittleEndian();
            obj.FirstExecutableFileEntryLength = data.ReadUInt32LittleEndian();
            obj.MsiFileEntryLength = data.ReadUInt32LittleEndian();

            // If the reported header information is invalid
            if (obj.Version == null)
                return obj;
            if (wisOffset < 0)
                return obj;
            if (headerLength < 0)
                return obj;

            if (headerLength > 6)
            {
                obj.UnknownValue7 = data.ReadUInt32LittleEndian();
                obj.UnknownValue8 = data.ReadUInt32LittleEndian();
            }

            if (headerLength > 8)
            {
                obj.ThirdExecutableFileEntryLength = data.ReadUInt32LittleEndian();
                obj.UnknownValue10 = data.ReadUInt32LittleEndian();
                obj.UnknownValue11 = data.ReadUInt32LittleEndian();
                obj.UnknownValue12 = data.ReadUInt32LittleEndian();
                obj.UnknownValue13 = data.ReadUInt32LittleEndian();
                obj.UnknownValue14 = data.ReadUInt32LittleEndian();
                obj.UnknownValue15 = data.ReadUInt32LittleEndian();
                obj.UnknownValue16 = data.ReadUInt32LittleEndian();
                obj.UnknownValue17 = data.ReadUInt32LittleEndian();
            }

            if (headerLength > 17)
            {
                obj.UnknownValue18 = data.ReadUInt32LittleEndian();
            }

            // If the WIS string has not been hit, read the padding bytes
            if (data.Position < initialOffset + wisOffset)
            {
                int paddingLength = (int)(initialOffset + wisOffset - data.Position);
                _ = data.ReadBytes(paddingLength);
            }

            // Read the consistent strings
            obj.TmpString = data.ReadNullTerminatedAnsiString();
            obj.GuidString = data.ReadNullTerminatedAnsiString();

            // Parse the pre-string section
            int preStringBytesSize = GetPreStringBytesSize(data, obj, wisOffset);
            if (preStringBytesSize <= 0)
                return obj;

            // Read the pre-string bytes
            obj.PreStringValues = data.ReadBytes(preStringBytesSize);

            // Try to read the string arrays
            // TODO: Count size of string section for later size verification
            byte[][]? stringArrays = ParseStringTable(data, obj.PreStringValues);
            if (stringArrays == null)
                return obj;

            // Set the string arrays
            obj.Strings = stringArrays;

            // Not sure what this data is. Might be a wisescript?
            if (obj.UnknownDataSize != 0)
                data.Seek(obj.UnknownDataSize, SeekOrigin.Current);

            return obj;
        }

        /// <summary>
        /// Get the pre-string bytes size, if possible
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="header">Section header to get information from</param>
        /// <param name="wisOffset">Offset to the WIS string relative to the start of the header</param>
        /// <returns>The size of the pre-string section</returns>
        /// <remarks>
        /// This method also sets <see cref="SectionHeader.NonWiseVersion"/> and
        /// <see cref="SectionHeader.PreFontValue"/>, if possible.
        /// </remarks>
        private static int GetPreStringBytesSize(Stream data, SectionHeader header, int wisOffset)
        {
            // Handle a case that shouldn't happen
            if (header.Version == null)
                return 0;

            // TODO: better way to figure out how far it's needed to advance?
            int versionSize;
            if (header.Version[header.Version.Length - 1] == 0x02)
                versionSize = header.Version[header.Version.Length - 3];
            else
                versionSize = header.Version[header.Version.Length - 2];

            // Third byte seems to indicate size of NonWiseVer
            if (versionSize > 1)
            {
                byte[] stringBytes = data.ReadBytes(versionSize);
                header.NonWiseVersion = Encoding.ASCII.GetString(stringBytes);
                if (wisOffset <= 77)
                    header.PreFontValue = data.ReadBytes(2);
                else
                    header.PreFontValue = data.ReadBytes(4);
            }

            // If that third byte is 0x01, no NonWiseVersion string is present
            else
            {
                header.PreFontValue = data.ReadBytes(3);
            }

            header.FontSize = data.ReadByte();
            int preStringBytesSize = WiseSectionPreStringBytesSize[wisOffset];

            // Hack for Codesited5.exe , very early and very strange.
            if (header.Version[1] == 0x01)
                preStringBytesSize = 2;

            return preStringBytesSize;
        }

        /// <summary>
        /// Parse the string table, if possible
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="preStringValues">Pre-string byte array containing string lengths</param>
        /// <returns>The filled string table on success, false otherwise</returns>
        private static byte[][]? ParseStringTable(Stream data, byte[] preStringValues)
        {
            // Setup the loop variables
            List<byte[]> stringList = [];
            int counter = 0;
            bool endNow = false;
            bool languageSection = false;
            int languageSectionCounter = 0;

            // Iterate pre-string byte array
            while (counter < preStringValues.Length)
            {
                // Read the next byte value
                byte currentByte = preStringValues[counter];
                if (currentByte == 0x00)
                    break;

                // Now doing third byte after language section begins
                if (currentByte == 0x01 && languageSectionCounter == 2)
                {
                    int extraLanguages = preStringValues[counter + 1];
                    for (int i = 0; i < extraLanguages; i++)
                    {
                        byte[]? incrementBytes = data.ReadBytes(2);
                        string? extraLanguageString = data.ReadNullTerminatedAnsiString();
                        if (extraLanguageString == null)
                            return null;

                        byte[]? extraLanguageStringArray = Encoding.ASCII.GetBytes(extraLanguageString);
                        stringList.Add(incrementBytes);
                        stringList.Add(extraLanguageStringArray);
                    }

                    break;
                }

                // Prepends non-string-size indicators
                else if (currentByte == 0x01)
                {
                    // 01 01 01 01: entering font section
                    // 01 5D 5C 01: link section; 5D and 5C are string sizes
                    int oneCount = 1;
                    counter++;
                    for (int i = counter; i <= preStringValues.Length; i++)
                    {
                        if (i == preStringValues.Length)
                        {
                            byte checkForZero;
                            do
                            {
                                checkForZero = data.ReadByteValue();
                            } while (checkForZero == 0x00);

                            data.Seek(-1, SeekOrigin.Current);
                            endNow = true;
                            break;
                        }

                        currentByte = preStringValues[counter];

                        // 0x01 followed by one more 0x01 seems to indicate to skip 2 null bytes, but 0x01 followed by
                        // three more 0x01 seems to indicate an unspecified length of null bytes that must be skipped.
                        // It has already been observed it mean 22 or 27 between 2 samples.

                        // If you encounter a null byte in the actual pre-string byte array, it seems to always be
                        // after you've read all the strings successfully.
                        if (currentByte == 0x00)
                        {
                            endNow = true;
                            break;
                        }
                        else if (currentByte > 0x01)
                        {
                            byte checkForZero;
                            do
                            {
                                checkForZero = data.ReadByteValue();
                            } while (checkForZero == 0x00);

                            data.Seek(-1, SeekOrigin.Current);
                            break;
                        }
                        else
                        {
                            oneCount++;
                        }
                        counter++;
                    }

                    if (oneCount == 4)
                        languageSection = true;
                }

                // If there was an issue
                if (endNow)
                    break;

                // Read and add the string as a byte array
                byte[] currentString = data.ReadBytes(currentByte);
                stringList.Add(currentString);

                counter++;
                if (languageSection)
                    languageSectionCounter++;
            }

            return [.. stringList];
        }
    }
}
