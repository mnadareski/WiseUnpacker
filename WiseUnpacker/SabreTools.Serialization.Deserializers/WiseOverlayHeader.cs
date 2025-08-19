using System;
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
                // Cache the current offset
                long initialOffset = data.Position;

                var overlayHeader = ParseOverlayHeader(data);

                // WiseColors.dib
                if (overlayHeader.DibDeflatedSize >= data.Length)
                    return null;
                else if (overlayHeader.DibDeflatedSize > overlayHeader.DibInflatedSize)
                    return null;

                // WiseScript.bin
                if (overlayHeader.WiseScriptDeflatedSize == 0)
                    return null;
                else if (overlayHeader.WiseScriptDeflatedSize >= data.Length)
                    return null;
                else if (overlayHeader.WiseScriptDeflatedSize > overlayHeader.WiseScriptInflatedSize)
                    return null;

                // WISE0001.DLL
                if (overlayHeader.WiseDllDeflatedSize >= data.Length)
                    return null;

                // FILE00XX.DAT
                if (overlayHeader.FinalFileDeflatedSize == 0)
                    return null;
                else if (overlayHeader.FinalFileDeflatedSize >= data.Length)
                    return null;
                else if (overlayHeader.FinalFileDeflatedSize > overlayHeader.FinalFileInflatedSize)
                    return null;

                // Valid for older overlay headers
                if (overlayHeader.Endianness == 0x0000 && overlayHeader.InitTextLen == 0)
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

            // Read as a single block
            header.Flags = (OverlayHeaderFlags)data.ReadUInt32LittleEndian();

            // Read as a single block
            header.GraphicsData = data.ReadBytes(12);

            // Read as a single block
            header.WiseScriptExitEventOffset = data.ReadUInt32LittleEndian();
            header.WiseScriptCancelEventOffset = data.ReadUInt32LittleEndian();

            // Read as a single block
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

            // Newer installers read this and DibInflatedSize in the above block
            header.DibDeflatedSize = data.ReadUInt32LittleEndian();

            // Handle older overlay data
            if (header.DibDeflatedSize > data.Length)
            {
                header.DibDeflatedSize = 0;
                data.Seek(-4, SeekOrigin.Current);
                return header;
            }

            header.DibInflatedSize = data.ReadUInt32LittleEndian();

            // Peek at the next 2 bytes
            ushort peek = data.ReadUInt16LittleEndian();
            data.Seek(-2, SeekOrigin.Current);

            // If the next value is a known Endianness
            if (Enum.IsDefined(typeof(Endianness), peek))
            {
                header.Endianness = (Endianness)data.ReadUInt16LittleEndian();
            }
            else
            {
                // The first two values are part of the sizes block above
                header.InstallScriptDeflatedSize = data.ReadUInt32LittleEndian();
                header.CharacterSet = (CharacterSet)data.ReadUInt32LittleEndian();
                header.Endianness = (Endianness)data.ReadUInt16LittleEndian();
            }

            // Endianness and init text len are read in a single block
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
