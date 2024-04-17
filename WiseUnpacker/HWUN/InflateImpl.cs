using System.IO;
using SabreTools.IO;

namespace WiseUnpacker.HWUN
{
    internal class InflateImpl : SecureInflate
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
        /// Inflation result
        /// </summary>
        public ushort Result { get; private set; }

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
            bool inflated = SI_INFLATE();
            inf.Seek(inf.Position - _bufferSize + _bufferPosition, SeekOrigin.Begin);
            CRC = CRC32.End(CRC);

            Result = SI_ERROR;
            _output.Close();
            return inflated;
        }

        public override byte? SI_READ()
        {
            if (_bufferPosition >= _bufferSize)
            {
                if (_input!.Position >= _input!.Length)
                {
                    SI_BREAK = true;
                    return null;
                }
                else
                {
                    if (_bufferSize > _input!.Length - _input!.Position)
                        _bufferSize = (ushort)(_input!.Length - _input!.Position);

                    _input!.Read(_buffer, 0, _bufferSize);
                    _bufferPosition = 0x0000;
                }
            }

            byte inflateread = _buffer[_bufferPosition];
            _inputSize++;
            _bufferPosition++;
            return inflateread;
        }

        public override void SI_WRITE(ushort amount)
        {
            _outputSize += amount;
            _output!.Write(SI_WINDOW, 0, amount);
            CRC = CRC32.Add(CRC, SI_WINDOW, amount);
        }
    }
}