using System;
using System.IO;

// TODO: Remove when IO is updated
namespace SabreTools.IO.Streams
{
    /// <summary>
    /// Stream representing a view into a source
    /// </summary>
    public class ViewStream : Stream
    {
        #region Properties

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => _source.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <summary>
        /// Filename from the source, if possible
        /// </summary>
        public string? Filename
        {
            get
            {
                // A subset of streams have a filename
                if (_source is FileStream fs)
                    return fs.Name;
                else if (_source is ViewStream vs)
                    return vs.Filename;

                return null;
            }
        }

        /// <inheritdoc/>
        public override long Length => _length;

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                // Handle 0-length sources
                if (_length <= 0)
                    return 0;

                return _source.Position - _initialPosition;
            }
            set
            {
                // Handle 0-length sources
                if (_length <= 0)
                {
                    _source.Position = 0;
                    return;
                }

                long position = value;

                // Handle out-of-bounds seeks
                if (position < 0)
                    position = 0;
                else if (position >= _length)
                    position = _length - 1;

                _source.Position = _initialPosition + position;
            }
        }

        #endregion

        #region Instance Variables

        /// <summary>
        /// Initial position within the underlying data
        /// </summary>
        protected long _initialPosition;

        /// <summary>
        /// Usable length in the underlying data
        /// </summary>
        protected long _length;

        /// <summary>
        /// Source data
        /// </summary>
        protected Stream _source;

        /// <summary>
        /// Lock object for reading from the source
        /// </summary>
        private readonly object _sourceLock = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Construct a new ViewStream from a Stream
        /// </summary>
        public ViewStream(Stream data, long offset)
        {
            if (!data.CanRead)
                throw new ArgumentException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            _source = data;
            _initialPosition = offset;
            _length = data.Length - offset;

            _source.Seek(_initialPosition, SeekOrigin.Begin);
        }

        /// <summary>
        /// Construct a new ViewStream from a Stream
        /// </summary>
        public ViewStream(Stream data, long offset, long length)
        {
            if (!data.CanRead)
                throw new ArgumentException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || offset + length > data.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            _source = data;
            _initialPosition = offset;
            _length = length;

            _source.Seek(_initialPosition, SeekOrigin.Begin);
        }

        /// <summary>
        /// Construct a new ViewStream from a byte array
        /// </summary>
        public ViewStream(byte[] data, long offset)
        {
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            long length = data.Length - offset;
            _source = new MemoryStream(data, (int)offset, (int)length);
            _initialPosition = 0;
            _length = length;

            _source.Seek(_initialPosition, SeekOrigin.Begin);
        }

        /// <summary>
        /// Construct a new ViewStream from a byte array
        /// </summary>
        public ViewStream(byte[] data, long offset, long length)
        {
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || offset + length > data.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            _source = new MemoryStream(data, (int)offset, (int)length);
            _initialPosition = 0;
            _length = length;

            _source.Seek(_initialPosition, SeekOrigin.Begin);
        }

        #endregion

        #region Data

        /// <summary>
        /// Check if a data segment is valid in the data source 
        /// </summary>
        /// <param name="offset">Position in the source</param>
        /// <param name="count">Length of the data to check</param>
        /// <returns>True if the positional data is valid, false otherwise</returns>
        public bool SegmentValid(long offset, long count)
        {
            if (offset < 0 || offset > Length)
                return false;
            if (count < 0 || offset + count > Length)
                return false;

            return true;
        }

        #endregion

        #region Stream Implementations

        /// <inheritdoc/>
        public override void Flush()
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Invalid cases always return 0
            if (buffer.Length == 0)
                return 0;
            if (offset < 0 || offset >= buffer.Length)
                return 0;
            if (count < 0 || offset + count > buffer.Length)
                return 0;

            // Short-circuit 0-byte reads
            if (count == 0)
                return 0;

            try
            {
                lock (_sourceLock)
                {
                    return _source.Read(buffer, offset, count);
                }

            }
            catch
            {
                // Absorb the error
                return 0;
            }
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            // Handle the "seek"
            switch (origin)
            {
                case SeekOrigin.Begin: Position = offset; break;
                case SeekOrigin.Current: Position += offset; break;
                case SeekOrigin.End: Position = _length + offset - 1; break;
                default: throw new ArgumentException($"Invalid value for {nameof(origin)}");
            }

            return Position;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotImplementedException();

        #endregion
    }
}