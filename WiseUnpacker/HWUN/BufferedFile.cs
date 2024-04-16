using System;
using System.IO;

namespace WiseUnpacker.HWUN
{
    /// <remarks>
    /// TODO: Determine if the buffering is beneficial in C# or if this can be removed entirely
    /// </remarks>
    internal unsafe class BufferedFile
    {
        #region Properties

        /// <summary>
        /// Indicates if there was an error on the last read operation
        /// </summary>
        public bool Error { get; private set; }

        /// <summary>
        /// Represents the size of the underlying file as a DWORD
        /// </summary>
        public uint FileSize => (uint)_handle.Length;

        #endregion

        #region Internal State

        /// <summary>
        /// Name of the file that was used for the backing stream
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// Backing stream for the buffered file, usually a FileStream
        /// </summary>
        private readonly Stream _handle;

        /// <summary>
        /// Position within the buffered file
        /// </summary>
        /// <remarks>This does not always match up with the position in the backing stream</remarks>
        private long _position;

        /// <summary>
        /// Internal buffer for reading
        /// </summary>
        private readonly byte[] _buffer = new byte[0x8000]; // [$0000..$7fff]

        /// <summary>
        /// Starting address for the currently filled buffer
        /// </summary>
        private long _bufferOrigin;

        #endregion

        /// <summary>
        /// Create a new BufferedFile from an input file path
        /// </summary>
        public BufferedFile(string fileName)
        {
            // The file must exist
            if (!File.Exists(fileName))
                throw new FileNotFoundException(nameof(fileName));

            _name = fileName;
            _handle = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _bufferOrigin = 0xffff0000; // TODO: Determine why this is not set to 0
            _position = 0x00000000;
            Error = false;
        }

        /// <summary>
        /// Close the underlying stream
        /// </summary>
        public void Close() => _handle.Close();

        /// <summary>
        /// Determines if the end of file has been reached
        /// </summary>
        public bool EOF()
        {
            return _position >= FileSize;
        }

        /// <summary>
        /// Read a byte from the current position
        /// </summary>
        /// <remarks>Sets Error if reading could not be performed</remarks>
        public byte ReadByte() => ReadByte(_position);

        /// <summary>
        /// Read a byte from <paramref name="pos"/>
        /// </summary>
        /// <remarks>Sets Error if reading could not be performed</remarks>
        public byte ReadByte(long pos)
        {
            // If the requested position is invalid
            if (!ValidPosition(pos))
            {
                Error = true;
                return 0;
            }

            // Fill the buffer if the current data isn't in memory
            if (!InMemory(pos, 1))
                FillBuffer(pos);

            // Increment the position and return the requested value
            _position++;
            return _buffer[_position - _bufferOrigin];
        }

        /// <summary>
        /// Read a WORD from the current position
        /// </summary>
        /// <remarks>Sets Error if reading could not be performed</remarks>
        public ushort ReadWORD() => ReadWORD(_position);

        /// <summary>
        /// Read a WORD from <paramref name="pos"/>
        /// </summary>
        public ushort ReadWORD(long pos)
        {
            // If the requested position is invalid
            if (!ValidPosition(pos))
            {
                Error = true;
                return 0;
            }

            // Fill the buffer if the current data isn't in memory
            if (!InMemory(pos, 2))
                FillBuffer(pos);

            // Increment the position and return the requested value
            _position += 2;
            return BitConverter.ToUInt16(_buffer, (int)(pos - _bufferOrigin));
        }

        /// <summary>
        /// Read a DWORD from the current position
        /// </summary>
        /// <remarks>Sets Error if reading could not be performed</remarks>
        public uint ReadDWORD() => ReadDWORD(_position);

        /// <summary>
        /// Read a DWORD from <paramref name="pos"/>
        /// </summary>
        /// <remarks>Sets Error if reading could not be performed</remarks>
        public uint ReadDWORD(long pos)
        {
            // If the requested position is invalid
            if (!ValidPosition(pos))
            {
                Error = true;
                return 0;
            }

            // Fill the buffer if the current data isn't in memory
            if (!InMemory(pos, 4))
                FillBuffer(pos);

            // Increment the position and return the requested value
            _position += 4;
            return BitConverter.ToUInt32(_buffer, (int)(pos - _bufferOrigin));
        }

        /// <summary>
        /// Seek to the given position without validation
        /// </summary>
        public void Seek(int pos)
        {
            _position = pos;
        }

        #region Helpers

        /// <summary>
        /// Determines if a position is valid for the given file
        /// </summary>
        private bool ValidPosition(long position)
        {
            return position >= 0 && position < _handle.Length;
        }

        /// <summary>
        /// Determines if a segment is fully contained in the buffer
        /// </summary>
        private bool InMemory(long position, int length)
        {
            return (position >= _bufferOrigin) && (position + length <= _bufferOrigin + 0x8000);
        }

        /// <summary>
        /// Fill the buffer from the underlying stream at <paramref name="position"/>
        /// </summary>
        private void FillBuffer(long position)
        {
            _bufferOrigin = position - 0x4000;
            if (_bufferOrigin < 0x0000)
                _bufferOrigin = 0x0000;
            if (_bufferOrigin + 0x8000 > _handle.Length)
                _bufferOrigin = _handle.Length - 0x8000;

            // filesize < 0x8000
            if (_bufferOrigin < 0)
            {
                _bufferOrigin = 0;
                _handle.Seek(_bufferOrigin, SeekOrigin.Begin);
                _handle.Read(_buffer, 0, (int)_handle.Length);
            }
            else
            {
                _handle.Seek(_bufferOrigin, SeekOrigin.Begin);
                _handle.Read(_buffer, 0, 0x8000);
            }

            _position = position;
        }

        #endregion
    }
}