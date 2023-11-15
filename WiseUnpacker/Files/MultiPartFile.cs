using System;
using System.IO;
using System.Text;

namespace WiseUnpacker.Files
{
    internal class MultiPartFile : Stream
    {
        /// <inheritdoc/>
        public override long Length => _length;
        private long _length;

        /// <inheritdoc/>
        public override long Position { get; set; }

        private readonly Stream _stream;

        private long _partStart;

        private long _partEnd;

        private MultiPartFile? _next;

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        private MultiPartFile(string name) : this(File.OpenRead(name)) { }

        /// <summary>
        /// Constructor
        /// </summary>
        private MultiPartFile(Stream stream)
        {
            _stream = stream;
            _partStart = 0;
            Position = 0;
            _partEnd = stream?.Length ?? 0 - 1;
            _length = _partEnd + 1;
            _next = null;
        }

        /// <summary>
        /// Safely create a new MultiPartFile from a file path
        /// </summary>
        public static MultiPartFile? Create(string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || !File.Exists(name))
                return null;
            
            return new MultiPartFile(name!);
        }

        /// <summary>
        /// Safely create a new MultiPartFile from a stream
        /// </summary>
        public static MultiPartFile? Create(Stream? stream)
        {
            if (stream == null || !stream.CanRead)
                return null;
            
            return new MultiPartFile(stream);
        }

        #endregion

        #region Appending

        /// <summary>
        /// Append a new MultiPartFile to the current one
        /// </summary>
        /// <param name="file">New file path to append</param>
        public bool Append(string? file) => Append(Create(file));

        /// <summary>
        /// Append a new MultiPartFile to the current one
        /// </summary>
        /// <param name="stream">Stream to append</param>
        public bool Append(Stream? stream) => Append(Create(stream));

        /// <summary>
        /// Append a new MultiPartFile to the current one
        /// </summary>
        /// <param name="mpf">New MultiPartFile to append</param>
        private bool Append(MultiPartFile? mpf)
        {
            // If the part is invalid, we can't append
            if (mpf == null)
                return false;

            // Find the current last part
            var mf = this;
            while (_next != null)
            {
                mf = mf!._next;
            }

            // Assign the new part as the new end
            mf!._next = mpf;
            mf._next._partStart = Length;
            mf._next._partEnd += Length;
            _length = mf._next._partEnd + 1;
            
            return true;
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

        /// <summary>
        /// Close all parts of this MultiPartFile
        /// </summary>
        public override void Close()
        {
            if (_next != null)
                _next.Close();

            _stream.Close();
        }

        /// <summary>
        /// Read from the MultiPartFile to a buffer
        /// </summary>
        public override int Read(byte[] x, int offset, int amount)
        {
            int bufpos;

            MultiPartFile mf = this;
            while (Position > mf._partEnd && mf._next != null)
            {
                mf = mf._next;
            }

            if (Position > mf._partEnd)
                return 0;

            mf._stream.Seek(Position - mf._partStart, SeekOrigin.Begin);
            if (mf._partEnd + 1 - Position >= amount)
            {
                mf._stream.Read(x, offset, amount);
                Position += amount;
            }
            else
            {
                byte[] buf = new byte[0xffff];
                bufpos = 0;
                do
                {
                    if (mf!._partEnd + 1 < Position + amount - bufpos)
                    {
                        mf._stream.Read(buf, bufpos, (int)(mf._partEnd + 1 - Position));
                        bufpos += (int)(mf._partEnd + 1 - Position);
                        Position = mf._partEnd + 1;
                        mf = mf._next!;
                    }
                    else
                    {
                        mf._stream.Read(buf, bufpos, amount - bufpos);
                        Position += amount - bufpos;
                        bufpos = amount;
                    }
                }
                while (bufpos != amount);

                Array.ConstrainedCopy(buf, 0, x, offset, amount);
            }

            return amount;
        }

        /// <summary>
        /// Read a byte from the MultiPartFile
        /// </summary>
        public override int ReadByte()
        {
            byte[] x = new byte[1];
            Read(x, 0, 1);
            return x[0];
        }

        /// <summary>
        /// Read a byte from the MultiPartFile
        /// </summary>
        public byte ReadByteValue()
        {
            byte[] x = new byte[1];
            Read(x, 0, 1);
            return x[0];
        }

        /// <summary>
        /// Read a byte array from the MultiPartFile
        /// </summary>
        public byte[] ReadBytes(int count)
        {
            byte[] x = new byte[count];
            Read(x, 0, count);
            return x;
        }

        /// <summary>
        /// Read a character from the MultiPartFile
        /// </summary>
        public char ReadChar()
        {
            byte[] x = new byte[1];
            Read(x, 0, 1);
            return (char)x[0];
        }

        /// <summary>
        /// Read a character array from the MultiPartFile
        /// </summary>
        public char[] ReadChars(int count)
        {
            byte[] x = new byte[count];
            Read(x, 0, count);
            return Encoding.Default.GetString(x).ToCharArray();
        }

        /// <summary>
        /// Read a short from the MultiPartFile
        /// </summary>
        public short ReadInt16()
        {
            byte[] x = new byte[2];
            Read(x, 0, 2);
            return BitConverter.ToInt16(x, 0);
        }

        /// <summary>
        /// Read a ushort from the MultiPartFile
        /// </summary>
        public ushort ReadUInt16()
        {
            byte[] x = new byte[2];
            Read(x, 0, 2);
            return BitConverter.ToUInt16(x, 0);
        }

        /// <summary>
        /// Read an int from the MultiPartFile
        /// </summary>
        public int ReadInt32()
        {
            byte[] x = new byte[4];
            Read(x, 0, 4);
            return BitConverter.ToInt32(x, 0);
        }

        /// <summary>
        /// Read a uint from the MultiPartFile
        /// </summary>
        public uint ReadUInt32()
        {
            byte[] x = new byte[4];
            Read(x, 0, 4);
            return BitConverter.ToUInt32(x, 0);
        }

        /// <summary>
        /// Read a long from the MultiPartFile
        /// </summary>
        public long ReadInt64()
        {
            byte[] x = new byte[8];
            Read(x, 0, 8);
            return BitConverter.ToInt64(x, 0);
        }

        /// <summary>
        /// Read a ulong from the MultiPartFile
        /// </summary>
        public ulong ReadUInt64()
        {
            byte[] x = new byte[8];
            Read(x, 0, 8);
            return BitConverter.ToUInt64(x, 0);
        }

        #region Stream Implementations

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override void Flush() => _stream.Flush();

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
