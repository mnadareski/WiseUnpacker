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

            // Cache the current offset
            try
            {
                // Cache the current offset
                long initialOffset = data.Position;

                var header = ParseWiseSectionHeader(data, initialOffset);
                if (header == null)
                    return null;
                
                // Main MSI file
                if (header.MsiFileEntryLength == 0)
                    return null;
                else if (header.MsiFileEntryLength >= data.Length)
                    return null;

                // First executable file
                if (header.FirstExecutableFileEntryLength >= data.Length)
                    return null;

                // Second executable file
                if (header.SecondExecutableFileEntryLength >= data.Length)
                    return null;

                return header;
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
            int localWisOffset = -1;

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
                localWisOffset = offset;
            }
            
            if (header.Version == null)
                return null;
            

            if (localWisOffset < initialOffset)
                return null;

            // If the header length couldn't be determined
            if (headerLength < 0)
                return null;

            //Seek back to the beginning of the section
            data.Seek(initialOffset, 0);

            // Read common values
            header.UnknownDataSize = data.ReadUInt32LittleEndian();
            header.SecondExecutableFileEntryLength = data.ReadUInt32LittleEndian();
            header.UnknownValue2 = data.ReadUInt32LittleEndian();
            header.UnknownValue3 = data.ReadUInt32LittleEndian();
            header.UnknownValue4 = data.ReadUInt32LittleEndian();
            header.FirstExecutableFileEntryLength = data.ReadUInt32LittleEndian();
            header.MsiFileEntryLength = data.ReadUInt32LittleEndian();

            if (headerLength > 6)
            {
                header.UnknownValue7 = data.ReadUInt32LittleEndian();
                header.UnknownValue8 = data.ReadUInt32LittleEndian();
            }

            if (headerLength > 8)
            {
                header.ThirdExecutableFileEntryLength = data.ReadUInt32LittleEndian();
                header.UnknownValue10 = data.ReadUInt32LittleEndian();
                header.UnknownValue11 = data.ReadUInt32LittleEndian();
                header.UnknownValue12 = data.ReadUInt32LittleEndian();
                header.UnknownValue13 = data.ReadUInt32LittleEndian();
                header.UnknownValue14 = data.ReadUInt32LittleEndian();
                header.UnknownValue15 = data.ReadUInt32LittleEndian();
                header.UnknownValue16 = data.ReadUInt32LittleEndian();
                header.UnknownValue17 = data.ReadUInt32LittleEndian(); 
            }
            
            if (headerLength > 17)
            {
                header.UnknownValue18 = data.ReadUInt32LittleEndian();
            }
            
            // Parse strings
            // TODO: Count size of string section for later size verification

            PreStringValuesHelper(data, header, initialOffset, localWisOffset, header.Version, out int preStringBytesSize);
            
            byte[][]? stringArrays = StringHelper(data, header, preStringBytesSize);
            if (stringArrays == null)
                return null;
            
            header.Strings = stringArrays;
            
            // Should really be done in the wrapper, but almost everything there is static so there's no good place
            if (header.UnknownDataSize != 0) // Not sure what this data is. Might be a wisescript?
            {
                data.Seek(data.Position + header.UnknownDataSize, 0);
            }

            return header;
        }

        /// <summary>
        /// Attempts to read the pre-string bytes that lay out how to read the strings.
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="header">Section header</param>
        /// <param name="initialOffset">Initial offset to use in address comparisons</param>
        /// <param name="localWisOffset">Offset of WIS string, used to determine some other offsets and values</param>
        /// <param name="version">What might be a version value for the .WISE installer</param>
        /// <param name="preStringBytesSize">Assumed size of the pre-string bytes. Currently, not always accurate.</param>
        /// <returns>True on success, false on failure.</returns>
        private static bool PreStringValuesHelper(Stream data, SectionHeader header, long initialOffset, int localWisOffset, byte[] version, out int preStringBytesSize)
        {
            data.Seek(initialOffset + localWisOffset, 0);
            header.TmpString = data.ReadNullTerminatedAnsiString();
            header.GuidString = data.ReadNullTerminatedAnsiString();
            // TODO: better way to figure out how far it's needed to advance?
            int versionSize;
            if (version[version.Length - 1] == 0x02)
                versionSize = version[version.Length - 3];
            else
                versionSize = version[version.Length - 2];
            
            if (versionSize <= 1) // third byte seems to indicate size of NonWiseVer
            {
                byte[] stringBytes = data.ReadBytes(versionSize);
                header.NonWiseVersion = Encoding.ASCII.GetString(stringBytes);
                if (localWisOffset <= 77)
                {
                    header.PreFontValue = data.ReadBytes(2);
                }
                else
                {
                    header.PreFontValue = data.ReadBytes(4);
                }
            }
            
            else // If that third byte is 0x01, no NonWiseVersion string is present.
            {
                header.PreFontValue = data.ReadBytes(3);
            }
            
            header.FontSize = data.ReadByte(); 
            preStringBytesSize = WiseSectionPreStringBytesSize[localWisOffset];
            if (version[1] == 0x01)
            {
                preStringBytesSize = 2; // hack for Codesited5.exe , very early and very strange.
            }
            
            return true;
        }

        /// <summary>
        /// Attempts to read the string section.
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="header">Section header</param>
        /// <param name="preStringBytesSize">Assumed size of the pre-string bytes. Currently, not always accurate.</param>
        /// <returns>Array of byte arrays representing strings on success, null on failure.</returns>
        private static byte[][]? StringHelper(Stream data, SectionHeader header, int preStringBytesSize)
        {
            header.PreStringValues = data.ReadBytes(preStringBytesSize);
            List<byte[]> stringList = new List<byte[]>(); // List of string bytes to be set to final value
            int counter = 0;
            bool endNow = false;
            bool languageSection = false;
            int languageSectionCounter = 0;
            while (counter < preStringBytesSize) // Iterate pre-string byte array
            {
                byte currentByte = header.PreStringValues[counter];
                if (languageSectionCounter == 2) // now doing third byte after language section begins
                {
                    if (currentByte == 0x00) // this should never happen.
                    {
                        endNow = true;
                        break;
                    }
                    else if (currentByte == 0x01) 
                    {
                        int extraLanguages = header.PreStringValues[counter + 1];
                        for (int i = 0; i < extraLanguages; i++)
                        {
                            byte[]? incrementBytes = data.ReadBytes(2);
                            string? extraLanguageString = data.ReadNullTerminatedAnsiString();
                            if (extraLanguageString == null) // this should never happen
                            {
                                return null;
                            }
                            byte[]? extraLanguageStringArray = Encoding.ASCII.GetBytes(extraLanguageString);
                            stringList.Add(incrementBytes);
                            stringList.Add(extraLanguageStringArray);
                        }
                        break;
                    }
                }
                else if (currentByte == 0x01) // Prepends non-string-size indicators
                {
                    // 01 01 01 01: entering font section
                    // 01 5D 5C 01: link section; 5D and 5C are string sizes
                    int oneCount = 1;
                    counter++;
                    for (int i = counter; i <= preStringBytesSize; i++)
                    {
                        if (i == preStringBytesSize)
                        {
                            
                            byte checkForZero = 0x00;
                            while (checkForZero == 0x00)
                            {
                                checkForZero = data.ReadByteValue();
                            }
                            data.Seek(data.Position - 1, 0);
                            endNow = true;
                            break;
                        }
                        currentByte = header.PreStringValues[counter];
                        
                        // 0x01 followed by one more 0x01 seems to indicate to skip 2 null bytes, but 0x01 followed by
                        // three more 0x01 seems to indicate an unspecified length of null bytes that must be skipped.
                        // It has already been observed it mean 22 or 27 between 2 samples.
                        
                        // If you encounter a null byte in the actual pre-string byte array, it seems to always be
                        // after you've read all the strings successfully.
                        if (currentByte == 0x00) // this should never happen
                        {
                            endNow = true; 
                            break;
                        }
                        else if (currentByte != 0x01)
                        {
                            byte checkForZero = 0x00;
                            while (checkForZero == 0x00)
                            {
                                checkForZero = data.ReadByteValue();
                            }

                            data.Seek(data.Position - 1, 0);
                            break;
                        }
                        else
                        {
                            oneCount++;
                        }
                        counter++;
                    }
                    if (oneCount == 4) 
                    {
                        languageSection = true;
                    }
                }
                else if (currentByte == 0x00) // this should never happen
                {
                    endNow = true;
                }
                if (endNow == true)
                    break;
                byte[] currentString = data.ReadBytes(currentByte); // System.Text.Encoding.ASCII.GetString(currentString);
                stringList.Add(currentString);
                counter++;
                if (languageSection)
                {
                    languageSectionCounter++;
                }
            }
            
            // Strings stored as byte array since one "string" can contain multiple null-terminated strings.
            return [.. stringList];
        }
    }
}
