using System.IO;
using System.IO.Compression;
using SabreTools.IO.Streams;

namespace WiseUnpacker.HWUN
{
    internal class InflateImpl
    {
        #region Properties

        /// <summary>
        /// Current number of bytes read from the input
        /// </summary>
        public long InputSize => _inputSize;

        /// <summary>
        /// Current number of bytes written to the output
        /// </summary>
        public long OutputSize => _outputSize;

        /// <summary>
        /// Calculated CRC for inflated data
        /// </summary>
        public uint CRC { get; set; }

        #endregion

        #region Internal State

        /// <summary>
        /// Source input for inflation
        /// </summary>
        private ReadOnlyCompositeStream? _input;

        /// <summary>
        /// Current number of bytes read from the input
        /// </summary>
        private long _inputSize;

        /// <summary>
        /// Output stream
        /// </summary>
        private Stream? _output;

        /// <summary>
        /// Current number of bytes written to the output
        /// </summary>
        private long _outputSize;

        /// <summary>
        /// Internal buffer for reading
        /// </summary>
        private readonly byte[] _buffer = new byte[0x4000];

        /// <summary>
        /// Current pointer to the internal buffer
        /// </summary>
        private ushort _bufferPosition;

        /// <summary>
        /// Size of the internal buffer
        /// </summary>
        private ushort _bufferSize;

        #endregion

        public bool Inflate(ReadOnlyCompositeStream inf, string outf)
        {
            _input = inf;
            _output = File.OpenWrite(outf);
            _inputSize = 0;
            _outputSize = 0;
            _bufferSize = (ushort)_buffer.Length;
            _bufferPosition = _bufferSize;

            CRC = CRC32.Start();
            try
            {
                long start = _input.Position;
                var ds = new DeflateStream(_input, CompressionMode.Decompress);
                while (true)
                {
                    byte[] buf = new byte[16 * 1024];
                    int read = ds.Read(buf, 0, buf.Length);
                    CRC = CRC32.Add(CRC, buf, (ushort)read);
                    _output.Write(buf, 0, read);

                    if (read < buf.Length)
                        break;
                }

                _inputSize = _input.Position - start;
                _outputSize = _output.Length;
            }
            catch
            {
                return false;
            }
            CRC = CRC32.End(CRC);

            _output.Close();
            return true;
        }
    }
}