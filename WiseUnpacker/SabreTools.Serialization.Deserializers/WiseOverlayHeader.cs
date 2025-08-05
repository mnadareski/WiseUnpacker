using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.Models.WiseInstaller;

namespace SabreTools.Serialization.Deserializers
{
    public class WiseOverlayHeader : BaseBinaryDeserializer<OverlayHeader>
    {
        /// <inheritdoc/>
        public override OverlayHeader? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                var overlayHeader = ParseOverlayHeader(data);

                if (overlayHeader.DibInflatedSize >= data.Length)
                    return null;
                if (overlayHeader.WiseScriptDeflatedSize >= data.Length)
                    return null;
                if (overlayHeader.FinalFileDeflatedSize >= data.Length)
                    return null;

                // Valid for older overlay headers
                    if (overlayHeader.Endianness == 0x0000)
                        return overlayHeader;
                if (overlayHeader.Endianness != Endianness.LittleEndian && overlayHeader.Endianness != Endianness.BigEndian)
                    return null;

                return overlayHeader;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into an OverlayHeader
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled OverlayHeader on success, null on error</returns>
        private static OverlayHeader ParseOverlayHeader(Stream data)
        {
            var header = new OverlayHeader();

            header.DllNameLen = data.ReadByteValue();
            if (header.DllNameLen > 0)
            {
                byte[] dllName = data.ReadBytes(header.DllNameLen);
                header.DllName = Encoding.ASCII.GetString(dllName);
                header.DllSize = data.ReadUInt32LittleEndian();
            }

            header.Flags = (OverlayHeaderFlags)data.ReadUInt32LittleEndian();
            header.UnknownBytes_1 = data.ReadBytes(2);
            header.StartGradient = data.ReadBytes(3);
            header.EndGradient = data.ReadBytes(3);
            header.UnknownBytes_2 = data.ReadBytes(4);
            header.WiseScriptNewEventOffset_1 = data.ReadUInt32LittleEndian();
            header.WiseScriptNewEventOffset_2 = data.ReadUInt32LittleEndian();
            header.WiseScriptInflatedSize = data.ReadUInt32LittleEndian();
            header.WiseScriptDeflatedSize = data.ReadUInt32LittleEndian();
            header.WiseDllDeflatedSize = data.ReadUInt32LittleEndian();
            header.Ctl3d32DeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData4DeflatedSize = data.ReadUInt32LittleEndian();
            header.RegToolDeflatedSize = data.ReadUInt32LittleEndian();
            header.ProgressDllDeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData7DeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData8DeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData9DeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData10DeflatedSize = data.ReadUInt32LittleEndian();
            header.FinalFileDeflatedSize = data.ReadUInt32LittleEndian();
            header.FinalFileInflatedSize = data.ReadUInt32LittleEndian();
            header.EOF = data.ReadUInt32LittleEndian();
            header.DibDeflatedSize = data.ReadUInt32LittleEndian();

            // Handle older overlay data
            if (header.DibDeflatedSize > data.Length)
            {
                header.DibDeflatedSize = 0;
                data.Seek(-4, SeekOrigin.Current);
                return header;
            }

            header.DibInflatedSize = data.ReadUInt32LittleEndian();
            header.Endianness = (Endianness)data.ReadUInt16LittleEndian();
            if (header.Endianness != Endianness.BigEndian && header.Endianness != Endianness.LittleEndian)
            {
                data.Seek(-2, SeekOrigin.Current);
                header.InstallScriptDeflatedSize = data.ReadUInt32LittleEndian();
                header.UnknownU32_4 = data.ReadUInt32LittleEndian();
                header.Endianness = (Endianness)data.ReadUInt16LittleEndian();
            }

            header.InitTextLen = data.ReadByteValue();
            if (header.InitTextLen > 0)
            {
                byte[] initText = data.ReadBytes(header.InitTextLen);
                header.InitText = Encoding.ASCII.GetString(initText);
            }

            return header;
        }
    }
}
