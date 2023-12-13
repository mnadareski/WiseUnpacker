using System;
using System.IO;

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
        private MultiPartFile(string name) : this(File.Open(name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }

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
            if (string.IsNullOrEmpty(name) || !File.Exists(name))
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

        /// <inheritdoc/>
        public override void Close()
        {
            _next?.Close();
            _stream.Close();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
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
            if (mf._partEnd + 1 - Position >= count)
            {
                mf._stream.Read(buffer, offset, count);
                Position += count;
            }
            else
            {
                byte[] buf = new byte[0xffff];
                bufpos = 0;
                do
                {
                    if (mf!._partEnd + 1 < Position + count - bufpos)
                    {
                        mf._stream.Read(buf, bufpos, (int)(mf._partEnd + 1 - Position));
                        bufpos += (int)(mf._partEnd + 1 - Position);
                        Position = mf._partEnd + 1;
                        mf = mf._next!;
                    }
                    else
                    {
                        mf._stream.Read(buf, bufpos, count - bufpos);
                        Position += count - bufpos;
                        bufpos = count;
                    }
                }
                while (bufpos != count);

                Array.ConstrainedCopy(buf, 0, buffer, offset, count);
            }

            return count;
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            byte[] x = new byte[1];
            Read(x, 0, 1);
            return x[0];
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
