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

                // Checks if version was able to be read
                if (header?.Version == null)
                    return null;

                // Main MSI file
                if (header.MsiFileEntryLength == 0)
                    return null;
                else if (header.MsiFileEntryLength >= data.Length)
                    return null;

                // First executable file
                if (header.FirstExecutableFileEntryLength == 0)
                    return null;
                else if (header.FirstExecutableFileEntryLength >= data.Length)
                    return null;

                // Second executable file
                if (header.SecondExecutableFileEntryLength == 0)
                    return null;
                else if (header.SecondExecutableFileEntryLength >= data.Length)
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

            // Parse strings
            // TODO: Count size of string section for later size verification
            
            data.Seek(initialOffset + localWisOffset, 0);
            header.TmpString = data.ReadNullTerminatedString(Encoding.ASCII); // read .TMP string
            header.GuidString = data.ReadNullTerminatedString(Encoding.ASCII); // read GUID string
            // TODO: Later installers have a version and another thing here that make it fail to parse.
            // TODO: Fix after basic functionality works
            header.FontSize = data.ReadUInt32(); // Endianness unknown. 
            int preStringBytesSize = WiseSectionPreStringBytesSize[localWisOffset];
            header.StringValues = data.ReadBytes(preStringBytesSize);
            List<byte> stringList = new List<byte>(); // List of string bytes to be set to final value
            int counter = 0;
            bool zeroByte = false;
            while (counter < preStringBytesSize) // Iterate pre-string byte array
            {
                byte currentByte = header.StringValues[counter];
                if (currentByte == 0x01) // Prepends non-string-size indicators
                {
                    counter++;
                    for (int i = counter; i < preStringBytesSize; i++)
                    {
                        // 0x01 followed by one more 0x01 seems to indicate to skip 2 null bytes, but 0x01 followed by
                        // three more 0x01 seems to indicate an unspecified length of null bytes that must be skipped.
                        // It has already been observed it mean 22 or 27 between 2 samples.
                        
                        // If you encounter a null byte in the actual pre-string byte array, it seems to always be
                        // after you've read all the strings successfully.
                        if (currentByte == 0x00)
                        {
                            zeroByte = true;
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
                        counter++;
                    }
                }
                if (zeroByte == true)
                    break;
                stringList.AddRange(data.ReadBytes(currentByte));
                counter++;
            }
            
            // Strings stored as byte array since one "string" can contain multiple null-terminated strings.
            header.Strings = stringList.ToArray(); 


            return header;
        }
    }
}
