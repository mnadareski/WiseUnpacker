using System;
using System.IO;

namespace WiseUnpacker.Files
{
    internal class ReadOnlyFile : Stream
    {
        /// <inheritdoc/>
        public override long Length => _length;
        private readonly long _length;

        /// <inheritdoc/>
        public override long Position { get; set; }

        private readonly Stream _stream;

        private readonly byte[] _buffer;

        private long _bufferOffset;

        public string Name { get; private set; }

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        private ReadOnlyFile(string name) : this(name, File.OpenRead(name)) { }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReadOnlyFile(string directory, string filename) : this(Path.Combine(directory, filename)) { }

        /// <summary>
        /// Constructor
        /// </summary>
        private ReadOnlyFile(string name, Stream stream)
        {
            _stream = stream;
            Name = name;
            _buffer = new byte[0x8000];
            _length = stream.Length;
            _bufferOffset = 0xffff0000;
            Position = 0;
        }

        #endregion

        #region Stream Wrappers

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return origin switch
            {
                SeekOrigin.Begin => Position = offset,
                _ => throw new NotImplementedException(),
            };
        }

        /// <inheritdoc/>
        public override void Close()
        {
            _stream.Close();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            long position = Position;

            int i = 0;
            for (; i < count; i++)
            {
                if (offset >= buffer.Length || EOF())
                    break;

                buffer[offset++] = ReadByte(position++);
            }

            return i;
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            return ReadByte(Position);
        }

        /// <summary>
        /// Read a byte from the ReadOnlyFile at a location
        /// </summary>
        public byte ReadByte(long p)
        {
            byte[] res = new byte[1];
            if (ValidPosition(p))
            {
                if (!InMemory(p, 1))
                    FillBuffer(p);

                Array.ConstrainedCopy(_buffer, (int)(p - _bufferOffset), res, 0, 1);
                Position++;
            }

            return res[0];
        }

        /// <summary>
        /// Determine if the stream is at the end of the file
        /// </summary>
        public bool EOF()
        {
            return Position >= Length;
        }

        public short ReadInt16(long p)
        {
            byte[] res = new byte[2];
            if (ValidPosition(p))
            {
                if (!InMemory(p, 2))
                    FillBuffer(p);

                Array.ConstrainedCopy(_buffer, (int)(p - _bufferOffset), res, 0, 2);
                Position++;
            }

            return BitConverter.ToInt16(res, 0);
        }

        public ushort ReadUInt16(long p)
        {
            byte[] res = new byte[2];
            if (ValidPosition(p))
            {
                if (!InMemory(p, 2))
                    FillBuffer(p);

                Array.ConstrainedCopy(_buffer, (int)(p - _bufferOffset), res, 0, 2);
                Position++;
            }

            return BitConverter.ToUInt16(res, 0);
        }

        public int ReadInt32(long p)
        {
            byte[] res = new byte[4];
            if (ValidPosition(p))
            {
                if (!InMemory(p, 4))
                    FillBuffer(p);

                Array.ConstrainedCopy(_buffer, (int)(p - _bufferOffset), res, 0, 4);
                Position++;
            }

            return BitConverter.ToInt32(res, 0);
        }

        public uint ReadUInt32(long p)
        {
            byte[] res = new byte[4];
            if (ValidPosition(p))
            {
                if (!InMemory(p, 4))
                    FillBuffer(p);

                Array.ConstrainedCopy(_buffer, (int)(p - _bufferOffset), res, 0, 4);
                Position++;
            }

            return BitConverter.ToUInt32(res, 0);
        }

        public DateTime ReadDateTime(long p)
        {
            ushort date = ReadUInt16(p + 0);
            ushort time = ReadUInt16(p + 2);
            return new DateTime(
                date / 0x200 + 1980,
                date % 0x200 / 0x20,
                date % 0x200 % 0x20,
                time / 0x800,
                time % 0x800 / 0x20,
                time % 0x800 % 0x20 * 2);
        }

        private bool ValidPosition(long p)
        {
            return p >= 0 && p < Length;
        }

        private bool InMemory(long p, long l)
        {
            return p >= _bufferOffset && p + l <= _bufferOffset + 0x8000;
        }

        private void FillBuffer(long p)
        {
            _bufferOffset = p - 0x4000;
            if (_bufferOffset < 0)
                _bufferOffset = 0;

            if (_bufferOffset + 0x8000 > Length)
                _bufferOffset = Length - 0x8000;

            // filesize < 0x8000
            if (_bufferOffset < 0)
            {
                _bufferOffset = 0;
                _stream.Seek(_bufferOffset, SeekOrigin.Begin);
                _stream.Read(_buffer, 0, (int)Length);
            }
            else
            {
                _stream.Seek(_bufferOffset, SeekOrigin.Begin);
                _stream.Read(_buffer, 0, 0x8000);
            }

            Position = p;
        }

        #endregion

        #region Stream Implementations

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override void Flush() => _stream.Flush();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        #endregion
    }
}
