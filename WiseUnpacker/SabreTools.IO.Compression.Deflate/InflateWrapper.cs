using System;
using System.IO;
using System.Text;
using SabreTools.Hashing;
using SabreTools.IO.Extensions;
using SabreTools.Models.PKZIP;
using static SabreTools.Models.PKZIP.Constants;

namespace SabreTools.IO.Compression.Deflate
{
    /// <summary>
    /// Wrapper to handle DEFLATE decompression with data verification
    /// </summary>
    public class InflateWrapper
    {
        #region Constants

        /// <summary>
        /// Buffer size for decompression
        /// </summary>
        private const int BufferSize = 1024 * 1024;

        #endregion

        #region Extraction

        /// <summary>
        /// Attempt to extract a file defined by a filename
        /// </summary>
        /// <param name="source">Stream representing the deflated data</param>
        /// <param name="filename">Output filename, null to auto-generate</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="expected">Expected DEFLATE stream information</param>
        /// <param name="pkzip">Indicates if PKZIP containers are used</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>Extraction status representing the final state</returns>
        /// <remarks>Assumes that the current stream position is where the compressed data lives</remarks>
        public static ExtractionStatus ExtractFile(Stream source,
            string? filename,
            string outputDirectory,
            DeflateInfo expected,
            bool pkzip,
            bool includeDebug)
        {
            // Debug output
            if (includeDebug) Console.WriteLine($"Attempting to extract {filename}");

            // Extract the file
            var destination = new MemoryStream();
            ExtractionStatus status = ExtractStream(source,
                destination,
                expected,
                pkzip,
                includeDebug,
                out var foundFilename);

            // If the extracted data is invalid
            if (status != ExtractionStatus.GOOD || destination == null)
                return status;

            // Ensure directory separators are consistent
            filename ??= foundFilename ?? $"FILE_[{expected.InputSize}, {expected.OutputSize}, {expected.Crc32}]";
            if (Path.DirectorySeparatorChar == '\\')
                filename = filename.Replace('/', '\\');
            else if (Path.DirectorySeparatorChar == '/')
                filename = filename.Replace('\\', '/');

            // Ensure the full output directory exists
            filename = Path.Combine(outputDirectory, filename);
            var directoryName = Path.GetDirectoryName(filename);
            if (directoryName != null && !Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            // Write the output file
            File.WriteAllBytes(filename, destination.ToArray());
            return status;
        }

        /// <summary>
        /// Attempt to extract a file to a stream
        /// </summary>
        /// <param name="source">Stream representing the deflated data</param>
        /// <param name="destination">Stream where the inflated data will be written</param>
        /// <param name="expected">Expected DEFLATE stream information</param>
        /// <param name="pkzip">Indicates if PKZIP containers are used</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="filename">Output filename if extracted from the data, null otherwise</param>
        /// <returns>Extraction status representing the final state</returns>
        /// <remarks>Assumes that the current stream position is where the compressed data lives</remarks>
        public static ExtractionStatus ExtractStream(Stream source,
            Stream destination,
            DeflateInfo expected,
            bool pkzip,
            bool includeDebug,
            out string? filename)
        {
            // If PKZIP containers are used
            if (pkzip)
                return ExtractStreamWithContainer(source, destination, expected, includeDebug, out filename);

            // If post-data checksums are used
            filename = null;
            return ExtractStreamWithChecksum(source, destination, expected, includeDebug);
        }

        /// <summary>
        /// Extract source data in a PKZIP container
        /// </summary>
        /// <param name="source">Stream representing the deflated data</param>
        /// <param name="destination">Stream where the inflated data will be written</param>
        /// <param name="expected">Expected DEFLATE stream information</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="filename">Filename from the PKZIP header, if it exists</param>
        /// <returns></returns>
        public static ExtractionStatus ExtractStreamWithContainer(Stream source,
            Stream destination,
            DeflateInfo expected,
            bool includeDebug,
            out string? filename)
        {
            // Set default values
            filename = null;

            // Debug output
            if (includeDebug) Console.WriteLine($"Offset: {source.Position:X8}, Expected Read: {expected.InputSize}, Expected Write: {expected.OutputSize}, Expected CRC-32: {expected.Crc32:X8}");

            // Check the validity of the inputs
            if (expected.InputSize == 0)
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read 0 bytes");
                return ExtractionStatus.INVALID;
            }
            else if (expected.InputSize > (source.Length - source.Position))
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read {expected.InputSize} bytes but only {source.Length - source.Position} bytes remain");
                return ExtractionStatus.INVALID;
            }

            // Cache the current offset
            long current = source.Position;

            // Parse the PKZIP header, if it exists
            LocalFileHeader? zipHeader = ParseLocalFileHeader(source);
            long zipHeaderBytes = source.Position - current;

            // Always trust the PKZIP CRC-32 value over what is supplied
            if (zipHeader != null)
                expected.Crc32 = zipHeader.CRC32;

            // If the filename is [NULL], replace with the zip filename
            if (zipHeader?.FileName != null)
            {
                filename = zipHeader.FileName;
                if (includeDebug) Console.WriteLine($"Filename from PKZIP header: {filename}");
            }

            // Debug output
            if (includeDebug) Console.WriteLine($"PKZIP Filename: {zipHeader?.FileName}, PKZIP Expected Read: {zipHeader?.CompressedSize}, PKZIP Expected Write: {zipHeader?.UncompressedSize}, PKZIP Expected CRC-32: {zipHeader?.CRC32:X4}");

            // Extract the file
            var actual = Inflate(source, destination);
            if (actual == null)
            {
                if (includeDebug) Console.Error.WriteLine($"Could not extract {filename}");
                return ExtractionStatus.FAIL;
            }

            // Account for the header bytes read
            actual.InputSize += zipHeaderBytes;
            source.Seek(current + actual.InputSize, SeekOrigin.Begin);

            // Verify the extracted data
            return VerifyExtractedData(source, current, expected, actual, includeDebug);
        }

        /// <summary>
        /// Extract source data with a trailing CRC-32 checksum
        /// </summary>
        /// <param name="source">Stream representing the deflated data</param>
        /// <param name="destination">Stream where the inflated data will be written</param>
        /// <param name="expected">Expected DEFLATE stream information</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns></returns>
        public static ExtractionStatus ExtractStreamWithChecksum(Stream source,
            Stream destination,
            DeflateInfo expected,
            bool includeDebug)
        {
            // Debug output
            if (includeDebug) Console.WriteLine($"Offset: {source.Position:X8}, Expected Read: {expected.InputSize}, Expected Write: {expected.OutputSize}, Expected CRC-32: {expected.Crc32:X8}");

            // Check the validity of the inputs
            if (expected.InputSize == 0)
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read 0 bytes");
                return ExtractionStatus.INVALID;
            }
            else if (expected.InputSize > (source.Length - source.Position))
            {
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract, expected to read {expected.InputSize} bytes but only {source.Length - source.Position} bytes remain");
                return ExtractionStatus.INVALID;
            }

            // Cache the current offset
            long current = source.Position;

            // Extract the file
            var actual = Inflate(source, destination);
            if (actual == null)
            {
                if (includeDebug) Console.Error.WriteLine($"Could not extract");
                return ExtractionStatus.FAIL;
            }

            // Seek to the true end of the data
            source.Seek(current + actual.InputSize, SeekOrigin.Begin);

            // If the read value is off-by-one after checksum
            if (actual.InputSize == expected.InputSize - 5)
            {
                // If not at the end of the file, get the corrected offset
                if (source.Position + 5 < source.Length)
                {
                    // TODO: What does this byte represent?
                    byte padding = source.ReadByteValue();
                    actual.InputSize += 1;

                    // Debug output
                    if (includeDebug) Console.WriteLine($"Off-by-one padding byte detected: 0x{padding:X2}");
                }
                else
                {
                    // Debug output
                    if (includeDebug) Console.WriteLine($"Not enough data to adjust offset");
                }
            }

            // If there is enough data to read the full CRC
            uint deflateCrc;
            if (source.Position + 4 < source.Length)
            {
                deflateCrc = source.ReadUInt32LittleEndian();
                actual.InputSize += 4;
            }
            // Otherwise, read what is possible and pad with 0x00
            else
            {
                byte[] deflateCrcBytes = new byte[4];
                int realCrcLength = source.Read(deflateCrcBytes, 0, (int)(source.Length - source.Position));

                // Parse as a little-endian 32-bit value
                deflateCrc = (uint)(deflateCrcBytes[0]
                            | (deflateCrcBytes[1] << 8)
                            | (deflateCrcBytes[2] << 16)
                            | (deflateCrcBytes[3] << 24));

                actual.InputSize += realCrcLength;
            }

            // If the CRC to check isn't set
            if (expected.Crc32 == 0)
                expected.Crc32 = deflateCrc;

            // Debug output
            if (includeDebug) Console.WriteLine($"DeflateStream CRC-32: {deflateCrc:X8}");

            // Verify the extracted data
            return VerifyExtractedData(source, current, expected, actual, includeDebug);
        }

        /// <summary>
        /// Parse a Stream into a local file header
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled local file header on success, null on error</returns>
        /// <remarks>Mirror of method in Serialization</remarks>
        private static LocalFileHeader? ParseLocalFileHeader(Stream data)
        {
            var header = new LocalFileHeader();

            header.Signature = data.ReadUInt32LittleEndian();
            if (header.Signature != LocalFileHeaderSignature)
                return null;

            header.Version = data.ReadUInt16LittleEndian();
            header.Flags = (GeneralPurposeBitFlags)data.ReadUInt16LittleEndian();
            header.CompressionMethod = (CompressionMethod)data.ReadUInt16LittleEndian();
            header.LastModifedFileTime = data.ReadUInt16LittleEndian();
            header.LastModifiedFileDate = data.ReadUInt16LittleEndian();
            header.CRC32 = data.ReadUInt32LittleEndian();
            header.CompressedSize = data.ReadUInt32LittleEndian();
            header.UncompressedSize = data.ReadUInt32LittleEndian();
            header.FileNameLength = data.ReadUInt16LittleEndian();
            header.ExtraFieldLength = data.ReadUInt16LittleEndian();

            if (header.FileNameLength > 0 && data.Position + header.FileNameLength <= data.Length)
            {
                byte[] filenameBytes = data.ReadBytes(header.FileNameLength);
                if (filenameBytes.Length != header.FileNameLength)
                    return null;

                header.FileName = Encoding.ASCII.GetString(filenameBytes);
            }
            if (header.ExtraFieldLength > 0 && data.Position + header.ExtraFieldLength <= data.Length)
            {
                byte[] extraBytes = data.ReadBytes(header.ExtraFieldLength);
                if (extraBytes.Length != header.ExtraFieldLength)
                    return null;

                header.ExtraField = extraBytes;
            }

            return header;
        }

        /// <summary>
        /// Verify the extracted stream data, seeking to the original location on failure
        /// </summary>
        /// <param name="source">Stream representing the deflated data</param>
        /// <param name="start">Position representing the start of the deflated data</param>
        /// <param name="expected">Expected deflation info</param>
        /// <param name="actual">Actual deflation info</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>Extraction status representing the final state</returns>
        private static ExtractionStatus VerifyExtractedData(Stream source,
            long start,
            DeflateInfo expected,
            DeflateInfo actual,
            bool includeDebug)
        {
            // Debug output
            if (includeDebug) Console.WriteLine($"Actual Read: {actual.InputSize}, Actual Write: {actual.OutputSize}, Actual CRC-32: {actual.Crc32:X8}");

            // If there's a mismatch during both reading and writing
            if (expected.InputSize >= 0 && expected.InputSize != actual.InputSize)
            {
                // This in/out check helps catch false positives, such as
                // files that have an off-by-one mismatch for read values
                // but properly match the output written values.

                // If the written bytes not correct as well
                if (expected.OutputSize >= 0 && expected.OutputSize != actual.OutputSize)
                {
                    if (includeDebug) Console.Error.WriteLine($"Mismatched read/write values!");
                    source.Seek(start, SeekOrigin.Begin);
                    return ExtractionStatus.WRONG_SIZE;
                }

                // If the written bytes are not being verified
                else if (expected.OutputSize < 0)
                {
                    if (includeDebug) Console.Error.WriteLine($"Mismatched read/write values!");
                    source.Seek(start, SeekOrigin.Begin);
                    return ExtractionStatus.WRONG_SIZE;
                }
            }

            // If there's just a mismatch during only writing
            if (expected.InputSize >= 0 && expected.InputSize == actual.InputSize)
            {
                // We want to log this but ignore the error
                if (expected.OutputSize >= 0 && expected.OutputSize != actual.OutputSize)
                {
                    if (includeDebug) Console.WriteLine($"Ignoring mismatched write values because read values match!");
                }
            }

            // Otherwise, the write size should be checked normally
            else if (expected.InputSize == 0 && expected.OutputSize >= 0 && expected.OutputSize != actual.OutputSize)
            {
                if (includeDebug) Console.Error.WriteLine($"Mismatched write values!");
                source.Seek(start, SeekOrigin.Begin);
                return ExtractionStatus.WRONG_SIZE;
            }

            // If there's a mismatch with the CRC-32
            if (expected.Crc32 != 0 && expected.Crc32 != actual.Crc32)
            {
                if (includeDebug) Console.Error.WriteLine($"Mismatched CRC-32 values!");
                source.Seek(start, SeekOrigin.Begin);
                return ExtractionStatus.BAD_CRC;
            }

            return ExtractionStatus.GOOD;
        }

        #endregion

        #region Inflation

        /// <summary>
        /// Inflate an input stream to an output stream
        /// </summary>
        /// <param name="source">Stream representing the deflated data</param>
        /// <param name="destination">Stream where the inflated data will be written</param>
        /// <returns>Deflate information representing the processed data on success, null on error</returns>
        public static DeflateInfo? Inflate(Stream source, Stream destination)
        {
            try
            {
                // Setup the hasher for CRC-32 calculation
                using var hasher = new HashWrapper(HashType.CRC32);

                // Create a DeflateStream from the input
                using var ds = new DeflateStream(source, CompressionMode.Decompress, leaveOpen: true);

                // Decompress in blocks
                while (true)
                {
                    byte[] buf = new byte[BufferSize];
                    int read = ds.Read(buf, 0, buf.Length);
                    if (read == 0)
                        break;

                    hasher.Process(buf, 0, read);
                    destination.Write(buf, 0, read);
                }

                // Finalize the hash
                hasher.Terminate();
                byte[] hashBytes = hasher.CurrentHashBytes!;

                // Save the deflate values
                return new DeflateInfo
                {
                    InputSize = ds.TotalIn,
                    OutputSize = ds.TotalOut,
                    Crc32 = BitConverter.ToUInt32(hashBytes, 0),
                };
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}