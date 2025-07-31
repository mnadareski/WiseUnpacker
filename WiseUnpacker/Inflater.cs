using System;
using System.IO;
using SabreTools.Compression.Deflate;
using SabreTools.Hashing;

namespace WiseUnpacker
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

            var crc = new HashWrapper(HashType.CRC32);
            try
            {
                long start = input.Position;
                var ds = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true);
                while (true)
                {
                    byte[] buf = new byte[1024];
                    int read = ds.Read(buf, 0, buf.Length);
                    if (read == 0)
                        break;

                    crc.Process(buf, 0, read);
                    output.Write(buf, 0, read);
                }

                // Save the deflate values
                InputSize = ds.TotalIn;
                OutputSize = ds.TotalOut;
            }
            catch
            {
                return false;
            }
            finally
            {
                output?.Close();
            }

            crc.Terminate();
            byte[] hashBytes = crc.CurrentHashBytes!;
            CRC = BitConverter.ToUInt32(hashBytes, 0);
            return true;
        }
    }
}