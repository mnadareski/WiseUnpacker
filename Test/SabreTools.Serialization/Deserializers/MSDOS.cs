using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.Models.MSDOS;
using static SabreTools.Models.MSDOS.Constants;

namespace SabreTools.Serialization.Deserializers
{
    public class MSDOS : BaseBinaryDeserializer<Executable>
    {
        /// <inheritdoc/>
        public override Executable? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long initialOffset = data.Position;

                // Create a new executable to fill
                var executable = new Executable();

                #region Executable Header

                // Try to parse the executable header
                var executableHeader = ParseExecutableHeader(data);
                if (executableHeader.Magic != SignatureString)
                    return null;

                // Set the executable header
                executable.Header = executableHeader;

                #endregion

                #region Relocation Table

                // If the offset for the relocation table doesn't exist
                long tableAddress = initialOffset + executableHeader.RelocationTableAddr;
                if (tableAddress >= data.Length)
                    return executable;

                // Try to parse the relocation table
                data.Seek(tableAddress, SeekOrigin.Begin);

                // Set the relocation table
                executable.RelocationTable = new RelocationEntry[executableHeader.RelocationItems];
                for (int i = 0; i < executableHeader.RelocationItems; i++)
                {
                    executable.RelocationTable[i] = ParseRelocationEntry(data);
                }

                #endregion

                // Return the executable
                return executable;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into an ExecutableHeader
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ExecutableHeader on success, null on error</returns>
        public static ExecutableHeader ParseExecutableHeader(Stream data)
        {
            var obj = new ExecutableHeader();

            #region Standard Fields

            byte[] magic = data.ReadBytes(2);
            obj.Magic = Encoding.ASCII.GetString(magic);
            obj.LastPageBytes = data.ReadUInt16LittleEndian();
            obj.Pages = data.ReadUInt16LittleEndian();
            obj.RelocationItems = data.ReadUInt16LittleEndian();
            obj.HeaderParagraphSize = data.ReadUInt16LittleEndian();
            obj.MinimumExtraParagraphs = data.ReadUInt16LittleEndian();
            obj.MaximumExtraParagraphs = data.ReadUInt16LittleEndian();
            obj.InitialSSValue = data.ReadUInt16LittleEndian();
            obj.InitialSPValue = data.ReadUInt16LittleEndian();
            obj.Checksum = data.ReadUInt16LittleEndian();
            obj.InitialIPValue = data.ReadUInt16LittleEndian();
            obj.InitialCSValue = data.ReadUInt16LittleEndian();
            obj.RelocationTableAddr = data.ReadUInt16LittleEndian();
            obj.OverlayNumber = data.ReadUInt16LittleEndian();

            #endregion

            // If we don't have enough data for PE extensions
            if (data.Position >= data.Length || data.Length - data.Position < 36)
                return obj;

            #region PE Extensions

            obj.Reserved1 = new ushort[4];
            for (int i = 0; i < obj.Reserved1.Length; i++)
            {
                obj.Reserved1[i] = data.ReadUInt16LittleEndian();
            }
            obj.OEMIdentifier = data.ReadUInt16LittleEndian();
            obj.OEMInformation = data.ReadUInt16LittleEndian();
            obj.Reserved2 = new ushort[10];
            for (int i = 0; i < obj.Reserved2.Length; i++)
            {
                obj.Reserved2[i] = data.ReadUInt16LittleEndian();
            }
            obj.NewExeHeaderAddr = data.ReadUInt32LittleEndian();

            #endregion

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an ExecutableHeader
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ExecutableHeader on success, null on error</returns>
        public static RelocationEntry ParseRelocationEntry(Stream data)
        {
            var obj = new RelocationEntry();

            obj.Offset = data.ReadUInt16LittleEndian();
            obj.Segment = data.ReadUInt16LittleEndian();

            return obj;
        }
    }
}
