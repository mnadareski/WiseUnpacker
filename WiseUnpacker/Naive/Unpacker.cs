using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Models.WiseInstaller;
using SabreTools.Serialization.Wrappers;
using static WiseUnpacker.Common;

namespace WiseUnpacker.Naive
{
    internal class Unpacker : IWiseUnpacker
    {
        #region Instance Variables

        /// <summary>
        /// Input file to read and extract
        /// </summary>
        private readonly ReadOnlyCompositeStream _inputFile;

        #endregion

        /// <summary>
        /// Create a new modified unpacker
        /// </summary>
        public Unpacker(string file)
        {
            if (!OpenFile(file, out var stream) || stream == null)
                throw new FileNotFoundException(nameof(file));

            _inputFile = stream;
        }

        /// <summary>
        /// Create a new modified unpacker
        /// </summary>
        public Unpacker(Stream stream)
        {
            // Default options
            _inputFile = new ReadOnlyCompositeStream(stream);
        }

        /// <inheritdoc/>
        public bool Run(string outputPath)
        {
            // Attempt to deserialize the file as either NE or PE
            var wrapper = WrapperFactory.CreateExecutableWrapper(_inputFile);
            if (wrapper is not NewExecutable && wrapper is not PortableExecutable)
                return false;

            // New Executable (NE)
            if (wrapper is NewExecutable nex)
            {
                // TODO: Implement NE processing
                Close();
                return false;
            }

            // Portable Executable (PE)
            else if (wrapper is PortableExecutable pex)
            {
                // Get the overlay offset
                int overlayOffset = pex.OverlayAddress;
                if (overlayOffset < 0 || overlayOffset >= _inputFile.Length)
                    return false;

                // Check there is any overlay data
                const int minHeaderLength = 92;
                if (overlayOffset + minHeaderLength >= _inputFile.Length)
                    return false;

                // Seek to the overlay
                _inputFile.Seek(overlayOffset, SeekOrigin.Begin);

                // Attempt to parse the overlay data as a header
                OverlayHeader header;
                try
                {
                    header = DeserializeOverlayHeader(_inputFile);

                    // Check that no parsed length is strange
                    if (header.DllNameLen >= _inputFile.Length)
                        return false;
                    if (header.WiseScriptDeflatedSize >= _inputFile.Length)
                        return false;
                    if (header.WiseDllDeflatedSize >= _inputFile.Length)
                        return false;
                    if (header.ProgressDllDeflatedSize >= _inputFile.Length)
                        return false;
                    if (header.SomeData6DeflatedSize >= _inputFile.Length)
                        return false;
                    if (header.SomeData7DeflatedSize >= _inputFile.Length)
                        return false;
                    if (header.SomeData5DeflatedSize >= _inputFile.Length)
                        return false;
                    if (header.DibDeflatedSize >= _inputFile.Length)
                        return false;
                    if (header.InitTextLen >= _inputFile.Length)
                        return false;
                }
                catch
                {
                    return false;
                }

                // Get if the format is PKZIP packed or not
#if NET20 || NET35
                bool pkzip = (header.Flags & OverlayHeaderFlags.WISE_FLAG_PK_ZIP) != 0;
#else
                bool pkzip = header.Flags.HasFlag(OverlayHeaderFlags.WISE_FLAG_PK_ZIP);
#endif

                // Extract and rename the files
                long offset = _inputFile.Position;
                int extracted = ExtractFiles(_inputFile, outputPath, pkzip, offset);
                RenameFiles(outputPath, extracted);

                // Close and return
                Close();
                return true;
            }

            // This should never happen
            Close();
            return false;
        }

        /// <summary>
        /// Deserialize the overlay header
        /// </summary>
        internal static OverlayHeader DeserializeOverlayHeader(Stream data)
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
            header.Unknown_20 = data.ReadBytes(20);
            header.WiseScriptInflatedSize = data.ReadUInt32LittleEndian();
            header.WiseScriptDeflatedSize = data.ReadUInt32LittleEndian();
            header.WiseDllDeflatedSize = data.ReadUInt32LittleEndian();
            header.UnknownU32_1 = data.ReadUInt32LittleEndian();
            header.UnknownU32_2 = data.ReadUInt32LittleEndian();
            header.UnknownU32_3 = data.ReadUInt32LittleEndian();
            header.ProgressDllDeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData6DeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData7DeflatedSize = data.ReadUInt32LittleEndian();
            header.Unknown_8 = data.ReadBytes(8);
            header.SomeData5DeflatedSize = data.ReadUInt32LittleEndian();
            header.SomeData5InflatedSize = data.ReadUInt32LittleEndian();
            header.EOF = data.ReadUInt32LittleEndian();
            header.DibDeflatedSize = data.ReadUInt32LittleEndian();

            // Handle older overlay data
            if (header.DibDeflatedSize > data.Length)
            {
                data.Seek(-4, SeekOrigin.Current);
                return header;
            }

            header.DibInflatedSize = data.ReadUInt32LittleEndian();
            header.Endianness = (Endianness)data.ReadUInt16LittleEndian();
            header.InitTextLen = data.ReadByteValue();
            if (header.InitTextLen > 0)
            {
                byte[] initText = data.ReadBytes(header.InitTextLen);
                header.InitText = Encoding.ASCII.GetString(initText);
            }

            return header;
        }

        /// <summary>
        /// Close the possible Wise installer
        /// </summary>
        private void Close()
        {
            _inputFile?.Close();
        }
    }
}