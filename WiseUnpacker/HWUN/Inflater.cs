using System.IO;
using System.IO.Compression;
using CRC32;
using SabreTools.IO.Streams;

namespace WiseUnpacker.HWUN
{
    internal class Inflater
    {
        #region Properties

        /// <summary>
        /// Current number of bytes read from the input
        /// </summary>
        public long InputSize { get; private set; }

        /// <summary>
        /// Current number of bytes written to the output
        /// </summary>
        public long OutputSize { get; private set; }

        /// <summary>
        /// Calculated CRC for inflated data
        /// </summary>
        public uint CRC { get; set; }

        #endregion

        /// <summary>
        /// Inflate an input stream to an output file path
        /// </summary>
        public bool Inflate(Stream input, string outputPath)
        {
            var output = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

            InputSize = 0;
            OutputSize = 0;
            CRC = 0;

            var crc = new OptimizedCRC();
            try
            {
                long start = input.Position;
                var ds = new DeflateStream(input, CompressionMode.Decompress);
                while (true)
                {
                    byte[] buf = new byte[1024];
                    int read = ds.Read(buf, 0, buf.Length);
                    crc.Update(buf, 0, read);
                    output.Write(buf, 0, read);

                    if (read == 0)
                        break;
                }

                // Set the potential size of the data
                InputSize = input.Position - start;
                OutputSize = output.Length;
            }
            catch
            {
                return false;
            }
            CRC = (uint)crc.Value;

            output?.Close();
            return true;
        }
    }
}