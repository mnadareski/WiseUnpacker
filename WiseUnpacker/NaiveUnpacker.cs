using System.IO;
using SabreTools.IO.Streams;
using SabreTools.Serialization.Wrappers;
using static WiseUnpacker.Common;

namespace WiseUnpacker
{
    internal class NaiveUnpacker : IWiseUnpacker
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
        public NaiveUnpacker(string file)
        {
            if (!OpenFile(file, out var stream) || stream == null)
                throw new FileNotFoundException(nameof(file));

            _inputFile = stream;
        }

        /// <summary>
        /// Create a new modified unpacker
        /// </summary>
        public NaiveUnpacker(Stream stream)
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
                WiseOverlayHeader header;
                try
                {
                    header = new WiseOverlayHeader(_inputFile);

                    // Check that no parsed length is strange
                    if (header.DllNameLength >= _inputFile.Length)
                        return false;
                    if (header.WiseScriptCompressedSize >= _inputFile.Length)
                        return false;
                    if (header.WiseDllCompressedSize >= _inputFile.Length)
                        return false;
                    if (header.ProgressDllCompressedSize >= _inputFile.Length)
                        return false;
                    if (header.Unknown6CompressedSize >= _inputFile.Length)
                        return false;
                    if (header.Unknown7CompressedSize >= _inputFile.Length)
                        return false;
                    if (header.FileDatCompressedSize >= _inputFile.Length)
                        return false;
                    if (header.DibCompressedSize >= _inputFile.Length)
                        return false;
                    if (header.InitTextLength >= _inputFile.Length)
                        return false;
                }
                catch
                {
                    return false;
                }

                // Get if the format is PKZIP packed or not
#if NET20 || NET35
                bool pkzip = (header.Flags & WiseOverlayHeaderFlags.PK_ZIP) != 0;
#else
                bool pkzip = header.Flags.HasFlag(WiseOverlayHeaderFlags.PK_ZIP);
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
        /// Close the possible Wise installer
        /// </summary>
        private void Close()
        {
            _inputFile?.Close();
        }
    }
}