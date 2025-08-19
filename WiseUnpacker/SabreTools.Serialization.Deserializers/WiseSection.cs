using System;
using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.Models.WiseInstaller;
using SabreTools.Matching;

namespace SabreTools.Serialization.Deserializers
{
    public class WiseSection : BaseBinaryDeserializer<WiseSectionHeader>
    {
        /// <inheritdoc/>
        public override WiseSectionHeader? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                var wiseSectionHeader = ParseWiseSectionHeader(data);

                // Checks if version was able to be read
                if (wiseSectionHeader.Version == null)
                    return null;

                // Main MSI file
                if (wiseSectionHeader.MSIFileEntryLength == 0)
                    return null;
                else if (wiseSectionHeader.MSIFileEntryLength >= data.Length)
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

                return wiseSectionHeader;
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
        /// <returns>Filled WiseSectionHeader on success, null on error</returns>
        private static WiseSectionHeader ParseWiseSectionHeader(Stream data)
        {
            var header = new WiseSectionHeader();
            int[] WISOffsets = new int[] {32, 33, 41, 77, 78, 82}; // Currently observed offsets for WIS
            byte[] WISstring = new byte[] {57, 49, 53}; // "WIS"
            int headerLength = -1; // indexed from 0
            // Find offset of "WIS", determine header length, read presumed version value
            foreach (int offset in WISOffsets)
            {
                data.Seek(offset, 0);
                if (data.ReadBytes(3).EqualsExactly(WISstring))
                {
                    // I assume there's a way to do this with matching statements, but c# matching statements don't seem
                    // to support multiple lines of code in the match, or setting variables equal to something.
                    switch(offset)
                    {
                        case 32:
                            headerLength = 6;
                            data.Seek(offset - 4, 0);
                            header.Version = data.ReadBytes(4);
                            break;
                        case 33:
                            headerLength = 6;
                            data.Seek(offset - 5, 0);
                            header.Version = data.ReadBytes(5);
                            break;
                        case 41:
                            headerLength = 8;
                            data.Seek(offset - 5, 0);
                            header.Version = data.ReadBytes(5);
                            break;
                        case 77:
                            headerLength = 17;
                            data.Seek(offset - 5, 0);
                            header.Version = data.ReadBytes(5);
                            break;
                        case 78:
                            headerLength = 17;
                            data.Seek(offset - 6, 0);
                            header.Version = data.ReadBytes(6);
                            break;
                        case 82:
                            headerLength = 18;
                            data.Seek(offset - 6, 0);
                            header.Version = data.ReadBytes(6);
                            break;
                        default: // how do I actually return null on error? It just gives me a compile error if I try.
                            headerLength = -1;
                            //return null; 
                            break;
                    }
                }
            }
            data.Seek(0, 0);
            // Is there a better way to handle returning at the proper time besides just hardcoding return checks?
            header.UnknownValue0 = data.ReadUInt32LittleEndian();
            header.SecondExecutableFileEntryLength = data.ReadUInt32LittleEndian();
            header.UnknownValue2 = data.ReadUInt32LittleEndian();
            header.UnknownValue3 = data.ReadUInt32LittleEndian();
            header.UnknownValue4 = data.ReadUInt32LittleEndian();
            header.FirstExecutableFileEntryLength = data.ReadUInt32LittleEndian();
            header.MSIFileEntryLength = data.ReadUInt32LittleEndian();
            if (headerLength == 6)
            {
                return header;
            }
            header.UnknownValue7 = data.ReadUInt32LittleEndian();
            header.UnknownValue8 = data.ReadUInt32LittleEndian();
            if (headerLength == 8)
            {
                return header;
            }
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
            {
                return header;
            }
            header.UnknownValue18 = data.ReadUInt32LittleEndian();
            if (headerLength == 18)
            {
                return header;
            }
            return header;
        }
    }
}
