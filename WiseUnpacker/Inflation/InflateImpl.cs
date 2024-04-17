using System;
using System.IO;
using SabreTools.IO.Streams;

namespace WiseUnpacker.Inflation
{
    internal class InflateImpl : Inflate
    {
        public long InputSize { get; private set; }

        public long OutputSize { get; private set; }

        public int Result { get; private set; }

        public uint CRC
        {
            get => _crc.Value;
            set => _crc.Value = value;
        }

        private readonly ReadOnlyCompositeStream _inputFile;

        private readonly Stream _outputFile;

        private byte[]? _inputBuffer;

        private int _inputBufferPosition;

        private int _inputBufferSize;

        private readonly CRC32 _crc;

        public InflateImpl(ReadOnlyCompositeStream inf, string outf)
        {
            _inputBuffer = new byte[0x4000];
            _inputFile = inf;
            _outputFile = File.OpenWrite(outf);
            InputSize = 0;
            OutputSize = 0;
            _inputBufferSize = _inputBuffer.Length;
            _inputBufferPosition = _inputBufferSize;
            _crc = new CRC32();
        }

        public override void SI_WRITE(int w)
        {
            OutputSize += w;
            _outputFile.Write(SI_WINDOW!, 0, w);
            _crc.Update(SI_WINDOW!, 0, w);
        }

        public override byte SI_READ()
        {
            if (_inputBufferPosition >= _inputBufferSize)
            {
                if (_inputFile.Position >= _inputFile.Length)
                {
                    SI_BREAK = true;
                    return Byte.MaxValue;
                }
                else
                {
                    if (_inputBufferSize > _inputFile.Length - _inputFile.Position)
                        _inputBufferSize = (int)(_inputFile.Length - _inputFile.Position);

                    _inputFile.Read(_inputBuffer!, 0, _inputBufferSize);
                    _inputBufferPosition = 0;
                }
            }

            byte result = _inputBuffer![_inputBufferPosition];
            InputSize++;
            _inputBufferPosition++;
            return result;
        }

        public void Close()
        {
            _inputFile.Seek(_inputFile.Position - _inputBufferSize + _inputBufferPosition, SeekOrigin.Begin);

            _crc.FinalizeValue();
            Result = SI_ERROR;

            _outputFile.Close();
            _inputBuffer = null;
        }
    }
}
