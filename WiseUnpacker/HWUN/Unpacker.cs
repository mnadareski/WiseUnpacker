using System;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using static WiseUnpacker.Common;

namespace WiseUnpacker.HWUN
{
    internal sealed class Unpacker : IWiseUnpacker
    {
        #region Instance Variables

        /// <summary>
        /// Input file to read and extract
        /// </summary>
        private readonly ReadOnlyCompositeStream _inputFile;

        /// <summary>
        /// Number of bytes to roll back if data is not PKZIP
        /// </summary>
        private uint _rollback;

        /// <summary>
        /// User-provided offset to start looking for data if not PKZIP
        /// </summary>
        private uint _userOffset;

        /// <summary>
        /// Determines if files will be renamed after extraction
        /// </summary>
        private bool _renaming;

        #endregion

        /// <summary>
        /// Create a new HWUN unpacker
        /// </summary>
        public Unpacker(string file)
        {
            // Input path(s)
            if (!OpenFile(file, out var stream) || stream == null)
                throw new FileNotFoundException(nameof(file));

            // Default options
            _inputFile = stream;
            _rollback = 0;
            unchecked { _userOffset = (uint)-1; }
            _renaming = true;
        }

        /// <summary>
        /// Create a new HWUN unpacker
        /// </summary>
        public Unpacker(Stream stream)
        {
            // Input stream
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                throw new ArgumentException(nameof(stream));

            // Default options
            _inputFile = new ReadOnlyCompositeStream(stream);
            _rollback = 0;
            unchecked { _userOffset = (uint)-1; }
            _renaming = true;
        }

        /// <summary>
        /// Create a new HWUN unpacker with options set
        /// </summary>
        public Unpacker(string file, string? options)
        {
            // Input path(s)
            if (!OpenFile(file, out var stream) || stream == null)
                throw new FileNotFoundException(nameof(file));

            // Default options
            _inputFile = stream;
            _rollback = 0;
            unchecked { _userOffset = (uint)-1; }
            _renaming = true;

            // User-provided options
            ParseOptions(options);
        }

        /// <summary>
        /// Create a new HWUN unpacker with options set
        /// </summary>
        public Unpacker(Stream stream, string? options)
        {
            // Input stream
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                throw new ArgumentException(nameof(stream));

            // Default options
            _inputFile = new ReadOnlyCompositeStream(stream);
            _rollback = 0;
            unchecked { _userOffset = (uint)-1; }
            _renaming = true;

            // User-provided options
            ParseOptions(options);
        }

        /// <inheritdoc/>
        public bool Run(string outputPath)
        {
            // Run the approximation
            long approxOffset = Approximate(_inputFile, out bool pkzip);

            // If the data is not PKZIP
            if (!pkzip)
            {
                // Reset the approximate offset
                if (_userOffset >= 0)
                    approxOffset = _userOffset;
                else
                    approxOffset -= _rollback;

                bool realFound = FindReal(_inputFile, outputPath, approxOffset, out long realOffset);
                if (realFound)
                {
                    int extracted = ExtractFiles(_inputFile, outputPath, pkzip, realOffset);
                    if (_renaming)
                        RenameFiles(outputPath, extracted);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // Use the approximate offset as the real offset
                int extracted = ExtractFiles(_inputFile, outputPath, pkzip, approxOffset);
                if (_renaming)
                    RenameFiles(outputPath, extracted);
            }

            _inputFile.Close();
            return true;
        }

        #region Helpers

        /// <summary>
        /// Approximate the location of the WISE information
        /// </summary>
        private static long Approximate(ReadOnlyCompositeStream input, out bool pkzip)
        {
            // Read the first 0xC000 bytes into a buffer
            byte[] buf = new byte[0xC200];
            input.Seek(0x0000, SeekOrigin.Begin);
            int read = input.Read(buf, 0, 0xC000);

            // Use the initial offset and search for non-zero values
            long approxOffset = 0xC000;
            while (((buf[approxOffset] != 0x00) || (buf[approxOffset + 1] != 0x00)) && approxOffset > 0x20)
            {
                // Decrement the offset
                approxOffset--;

                // If either value is non-zero, keep searching
                if (buf[approxOffset] != 0x00 || buf[approxOffset + 1] != 0x00)
                    continue;

                // Find the current number of zeroes
                int count = 0;
                for (int i = 0x01; i <= 0x20; i++)
                {
                    if (buf[approxOffset - i] == 0x00)
                        count++;
                }

                // If we have less than 4 zeroes, decrement and continue
                if (count < 0x04)
                    approxOffset -= 2;
            }

            // Move the approximate offset forward
            approxOffset += 2;

            // Move forward until we find a potential starting point
            while (buf[approxOffset + 3] == 0x00 && approxOffset + 4 < 0xC000)
            {
                approxOffset += 4;
            }

            // Read the potential real offset from the buffer
            if (buf[approxOffset] <= 0x20 && buf[approxOffset + 1] > 0x00 && buf[approxOffset + 1] + approxOffset + 3 < 0xC000)
            {
                uint value = (uint)(buf[approxOffset + 1] + 0x02);
                int count = 0x00;
                for (int i = 0x02; i <= value - 0x01; i++)
                {
                    if (buf[approxOffset + i] >= 0x80)
                        count++;
                }

                if (count * 0x100 / value < 0x10)
                    approxOffset += value;
            }

            // Search for a zip signature
            long offset = 0x02;
            uint signature = 0x00;
            while (signature != 0x04034B50 && offset < 0x80 && approxOffset - offset >= 0 && approxOffset - offset <= 0xBFFC)
            {
                signature = BitConverter.ToUInt32(buf, (int)(approxOffset - offset));
                offset++;
            }

            // If the zip signature was found
            if (offset < 0x80)
            {
                pkzip = true;
                offset = 0x00;
                signature = 0x00;

                // Double-check the signature value is correctly set
                // TODO: Determine why this secondary check is done
                while (signature != 0x04034B50 && offset < approxOffset)
                {
                    signature = BitConverter.ToUInt32(buf, (int)offset);
                    offset++;
                }

                // Decrement the offset and set the new approximate offset
                offset--;
                approxOffset = offset;
                if (signature != 0x04034B50)
                    pkzip = false;
            }
            else
            {
                pkzip = false;
            }

            return approxOffset;
        }

        /// <summary>
        /// Find the real offset for non-zipped contents
        /// </summary>
        private static bool FindReal(ReadOnlyCompositeStream input, string outputPath, long approxOffset, out long realOffset)
        {
            // Create the output directory to extract to
            Directory.CreateDirectory(outputPath);

            realOffset = 0x00;
            uint newcrc = 0;
            long pos;
            var inflater = new Inflater();

            // Reset the offset if out of bounds
            if (approxOffset < 0x100)
                approxOffset = 0x100;
            else if (approxOffset > 0xBF00)
                approxOffset = 0xBF00;

            // If the approximate offset is a valid WORD value
            if (approxOffset >= 0x0000 && approxOffset <= 0xFFFF)
            {
                // Attempt to find the real first file by inflating blocks
                pos = 0x0000;
                bool inflated;
                do
                {
                    input.Seek(approxOffset + pos, SeekOrigin.Begin);
                    inflated = inflater.Inflate(input, Path.Combine(outputPath, "WISE0001"));
                    newcrc = input.ReadUInt32LittleEndian();
                    realOffset = approxOffset + pos;
                    pos++;
                } while ((!inflated || newcrc == 0x00000000) && pos != 0x100);

                // Try to find the ending position based on a valid CRC
                if ((newcrc == 0x00000000) && pos == 0x100)
                {
                    pos = -1;
                    do
                    {
                        input.Seek(approxOffset + pos, SeekOrigin.Begin);
                        inflated = inflater.Inflate(input, Path.Combine(outputPath, "WISE0001"));
                        newcrc = input.ReadUInt32LittleEndian();
                        realOffset = approxOffset + pos;
                        pos--;
                    } while ((!inflated || newcrc == 0x00000000) && pos != -0x100);
                }
            }
            else
            {
                pos = -0x100;
            }

            // Check for indicators of no WISE installer
            if ((newcrc == 0x00000000) && pos == -0x100)
            {
                realOffset = 0xFFFFFFFF;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Convert a hex string to a DWORD value
        /// </summary>
        private static uint HexStringToDWORD(string value)
        {
            try
            {
                return uint.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Parse options from an input string
        /// </summary>
        private void ParseOptions(string? options)
        {
            // Ignore empty options
            if (string.IsNullOrEmpty(options))
                return;

            int b = 1;
            while (b <= options!.Length)
            {
                if (char.ToUpperInvariant(options[b]) == 'B')
                {
                    _rollback = HexStringToDWORD(options.Substring(b + 1, 4));
                    b += 4;
                }
                else if (char.ToUpperInvariant(options[b]) == 'U')
                {
                    _userOffset = HexStringToDWORD(options.Substring(b + 1, 4));
                    b += 4;
                }
                else if (char.ToUpperInvariant(options[b]) == 'R')
                {
                    _renaming = false;
                }

                b++;
            }
        }

        #endregion
    }
}