using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SabreTools.Hashing;
using SabreTools.IO.Compression.Deflate;
using SabreTools.IO.Extensions;
using SabreTools.Models.WiseInstaller;
using SabreTools.Models.WiseInstaller.Actions;

namespace SabreTools.Serialization.Wrappers
{
    public class WiseScript : WrapperBase<ScriptFile>
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Wise Installer Script File";

        #endregion

        #region Extension Properties

        /// <inheritdoc cref="ScriptFile.States"/>
        public MachineState[]? States => Model.States;

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public WiseScript(ScriptFile? model, byte[]? data, int offset)
            : base(model, data, offset)
        {
            // All logic is handled by the base class
        }

        /// <inheritdoc/>
        public WiseScript(ScriptFile? model, Stream? data)
            : base(model, data)
        {
            // All logic is handled by the base class
        }

        /// <summary>
        /// Create a Wise installer script file from a byte array and offset
        /// </summary>
        /// <param name="data">Byte array representing the script</param>
        /// <param name="offset">Offset within the array to parse</param>
        /// <returns>A Wise installer script file wrapper on success, null on failure</returns>
        public static WiseScript? Create(byte[]? data, int offset)
        {
            // If the data is invalid
            if (data == null || data.Length == 0)
                return null;

            // If the offset is out of bounds
            if (offset < 0 || offset >= data.Length)
                return null;

            // Create a memory stream and use that
            var dataStream = new MemoryStream(data, offset, data.Length - offset);
            return Create(dataStream);
        }

        /// <summary>
        /// Create a Wise installer script file from a Stream
        /// </summary>
        /// <param name="data">Stream representing the script</param>
        /// <returns>A Wise installer script file wrapper on success, null on failure</returns>
        public static WiseScript? Create(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                var mkb = Deserializers.WiseScript.DeserializeStream(data);
                if (mkb == null)
                    return null;

                return new WiseScript(mkb, data);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Extraction

        /// <summary>
        /// Process the state machine and perform all required actions
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="sourceDirectory">Directory where installer files live, if possible</param>
        /// <param name="script">Parsed script to retrieve information from</param>
        /// <param name="dataStart">Start of the deflated data</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="isPkzip">Indicates if PKZIP containers are used</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if there were no errors during processing, false otherwise</returns>
        public bool ProcessStateMachine(Stream data,
            string? sourceDirectory,
            long dataStart,
            string outputDirectory,
            bool isPkzip,
            bool includeDebug)
        {
            // If the state machine is invalid
            if (States == null || States.Length == 0)
                return false;

            // Initialize important loop information
            int normalFileCount = 0;
            Dictionary<string, string> environment = [];
            if (sourceDirectory != null)
                environment.Add("INST", sourceDirectory);

            // Loop through the state machine and process
            foreach (var state in States)
            {
                switch (state.Op)
                {
                    case OperationCode.InstallFile:
                        if (state.Data is not InstallFile fileHeader)
                            return false;

                        // Try to extract to the output directory
                        ExtractFile(data, dataStart, fileHeader, ++normalFileCount, outputDirectory, isPkzip, includeDebug);
                        break;

                    case OperationCode.EditIniFile:
                        if (state.Data is not EditIniFile editIniFile)
                            return false;

                        // Ensure directory separators are consistent
                        string iniFilePath = editIniFile.Pathname ?? $"WISE{normalFileCount:X4}.ini";
                        if (Path.DirectorySeparatorChar == '\\')
                            iniFilePath = iniFilePath.Replace('/', '\\');
                        else if (Path.DirectorySeparatorChar == '/')
                            iniFilePath = iniFilePath.Replace('\\', '/');

                        // Ignore path replacements
                        iniFilePath = iniFilePath.Replace("%", string.Empty);

                        // Ensure the full output directory exists
                        iniFilePath = Path.Combine(outputDirectory, iniFilePath);
                        var directoryName = Path.GetDirectoryName(iniFilePath);
                        if (directoryName != null && !Directory.Exists(directoryName))
                            Directory.CreateDirectory(directoryName);

                        using (var iniFile = File.Open(iniFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            iniFile.Write(Encoding.ASCII.GetBytes($"[{editIniFile.Section}]\n"));
                            iniFile.Write(Encoding.ASCII.GetBytes($"{editIniFile.Values ?? string.Empty}\n"));
                            iniFile.Flush();
                        }

                        break;

                    case OperationCode.UnknownDeflatedFile0x06:
                        if (state.Data is not Unknown0x06 unknown0x06)
                            return false;

                        // Try to extract to the output directory
                        ExtractFile(data, dataStart, unknown0x06, ++normalFileCount, outputDirectory, isPkzip, includeDebug);
                        break;

                    case OperationCode.DeleteFile:
                        if (state.Data is not DeleteFile deleteFile)
                            return false;

                        if (includeDebug) Console.WriteLine($"File {deleteFile.Pathname} is supposed to be deleted");
                        break;

                    case OperationCode.CreateDirectory:
                        if (state.Data is not CreateDirectory createDirectory)
                            return false;
                        if (createDirectory.Pathname == null)
                            return false;

                        try
                        {
                            if (includeDebug) Console.WriteLine($"Directory {createDirectory.Pathname} is being created");

                            // Ensure directory separators are consistent
                            string newDirectoryName = Path.Combine(outputDirectory, createDirectory.Pathname);
                            if (Path.DirectorySeparatorChar == '\\')
                                newDirectoryName = newDirectoryName.Replace('/', '\\');
                            else if (Path.DirectorySeparatorChar == '/')
                                newDirectoryName = newDirectoryName.Replace('\\', '/');

                            // Perform path replacements
                            foreach (var kvp in environment)
                            {
                                newDirectoryName = newDirectoryName.Replace($"%{kvp.Key}%", kvp.Value);
                            }

                            newDirectoryName = newDirectoryName.Replace("%", string.Empty);

                            // Remove wildcards from end of the path
                            if (newDirectoryName.EndsWith("*.*"))
                                newDirectoryName = newDirectoryName.Substring(0, newDirectoryName.Length - 4);

                            Directory.CreateDirectory(newDirectoryName);
                        }
                        catch
                        {
                            if (includeDebug) Console.WriteLine($"Directory {createDirectory.Pathname} could not be created!");
                        }
                        break;

                    case OperationCode.CopyLocalFile:
                        if (state.Data is not CopyLocalFile copyLocalFile)
                            return false;
                        if (copyLocalFile.Source == null)
                            return false;
                        if (copyLocalFile.Destination == null)
                            return false;

                        try
                        {
                            if (includeDebug) Console.WriteLine($"File {copyLocalFile.Source} is being copied to {copyLocalFile.Destination}");

                            // Ensure directory separators are consistent
                            string oldFilePath = copyLocalFile.Source;
                            if (Path.DirectorySeparatorChar == '\\')
                                oldFilePath = oldFilePath.Replace('/', '\\');
                            else if (Path.DirectorySeparatorChar == '/')
                                oldFilePath = oldFilePath.Replace('\\', '/');

                            // Perform path replacements
                            foreach (var kvp in environment)
                            {
                                oldFilePath = oldFilePath.Replace($"%{kvp.Key}%", kvp.Value);
                            }

                            oldFilePath = oldFilePath.Replace("%", string.Empty);

                            // Sanity check
                            if (!File.Exists(oldFilePath))
                            {
                                if (includeDebug) Console.WriteLine($"File {copyLocalFile.Source} is supposed to be copied to {copyLocalFile.Destination}, but it does not exist!");
                                break;
                            }

                            // Ensure directory separators are consistent
                            string newFilePath = Path.Combine(outputDirectory, copyLocalFile.Destination);
                            if (Path.DirectorySeparatorChar == '\\')
                                newFilePath = newFilePath.Replace('/', '\\');
                            else if (Path.DirectorySeparatorChar == '/')
                                newFilePath = newFilePath.Replace('\\', '/');

                            // Perform path replacements
                            foreach (var kvp in environment)
                            {
                                newFilePath = newFilePath.Replace($"%{kvp.Key}%", kvp.Value);
                            }

                            newFilePath = newFilePath.Replace("%", string.Empty);

                            // Sanity check
                            string? newFileDirectory = Path.GetDirectoryName(newFilePath);
                            if (newFileDirectory != null && !Directory.Exists(newFileDirectory))
                                Directory.CreateDirectory(newFileDirectory);

                            File.Copy(oldFilePath, newFilePath);
                        }
                        catch
                        {
                            if (includeDebug) Console.WriteLine($"File {copyLocalFile.Source} could not be copied!");
                        }

                        break;

                    case OperationCode.CustomDialogSet:
                        if (state.Data is not CustomDialogSet customDialogSet)
                            return false;

                        // Try to extract to the output directory
                        ++normalFileCount;
                        ExtractFile(data, dataStart, customDialogSet, outputDirectory, isPkzip, includeDebug);
                        break;

                    case OperationCode.GetTemporaryFilename:
                        if (state.Data is not GetTemporaryFilename getTemporaryFilename)
                            return false;

                        if (getTemporaryFilename.Variable != null)
                            environment[getTemporaryFilename.Variable] = Guid.NewGuid().ToString();
                        break;

                    case OperationCode.AddTextToInstallLog:
                        if (state.Data is not AddTextToInstallLog addTextToInstallLog)
                            return false;

                        if (includeDebug) Console.WriteLine($"INSTALL.LOG: {addTextToInstallLog.Text}");
                        break;

                    default:
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Attempt to extract a file defined by a filename
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="filename">Output filename, null to auto-generate</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="expectedBytesRead">Expected number of bytes to read during inflation, -1 to ignore</param>
        /// <param name="expectedBytesWritten">Expected number of bytes to written during inflation, -1 to ignore</param>
        /// <param name="expectedCrc">Expected CRC-32 of the output file, 0 to ignore</param>
        /// <param name="isPkzip">Indicates if PKZIP containers are used</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>Extraction status representing the final state</returns>
        /// <remarks>Assumes that the current stream position is where the compressed data lives</remarks>
        public static WiseExtractStatus ExtractFile(Stream data,
            string? filename,
            string outputDirectory,
            long expectedBytesRead,
            long expectedBytesWritten,
            uint expectedCrc,
            bool isPkzip,
            bool includeDebug)
        {
            // Ensure directory separators are consistent
            filename ??= "[NULL]";
            if (Path.DirectorySeparatorChar == '\\')
                filename = filename.Replace('/', '\\');
            else if (Path.DirectorySeparatorChar == '/')
                filename = filename.Replace('\\', '/');

            // Ensure the full output directory exists
            filename = Path.Combine(outputDirectory, filename);
            var directoryName = Path.GetDirectoryName(filename);
            if (directoryName != null && !Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            // Extract the file
            WiseExtractStatus status = ExtractStream(data,
                ref filename,
                expectedBytesRead,
                expectedBytesWritten,
                expectedCrc,
                isPkzip,
                includeDebug,
                out var extracted);
            if (extracted != null)
                File.WriteAllBytes(filename, extracted.ToArray());

            return status;
        }

        /// <summary>
        /// Attempt to extract a file to a stream
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="filename">Output filename, if one exists</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="expectedBytesRead">Expected number of bytes to read during inflation, -1 to ignore</param>
        /// <param name="expectedBytesWritten">Expected number of bytes to written during inflation, -1 to ignore</param>
        /// <param name="expectedCrc">Expected CRC-32 of the output file, 0 to ignore</param>
        /// <param name="isPkzip">Indicates if PKZIP containers are used</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="extracted">Output stream representing the extracted data, null on error</param>
        /// <returns>Extraction status representing the final state</returns>
        /// <remarks>Assumes that the current stream position is where the compressed data lives</remarks>
        public static WiseExtractStatus ExtractStream(Stream data,
            ref string filename,
            long expectedBytesRead,
            long expectedBytesWritten,
            uint expectedCrc,
            bool isPkzip,
            bool includeDebug,
            out MemoryStream? extracted)
        {
            // Debug output
            if (includeDebug) Console.WriteLine($"Offset: {data.Position:X8}, Filename: {filename}, Expected Read: {expectedBytesRead}, Expected Write: {expectedBytesWritten}, Expected CRC-32: {expectedCrc:X8}");

            // Check the validity of the inputs
            if (expectedBytesRead == 0)
            {
                extracted = null;
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract {filename}, expected to read 0 bytes");
                return WiseExtractStatus.INVALID;
            }
            else if (expectedBytesRead > (data.Length - data.Position))
            {
                extracted = null;
                if (includeDebug) Console.Error.WriteLine($"Not attempting to extract {filename}, expected to read {expectedBytesRead} bytes but only {data.Length - data.Position} bytes remain");
                return WiseExtractStatus.INVALID;
            }

            // Cache the current offset
            long current = data.Position;

            // Skip the PKZIP header, if it exists
            Models.PKZIP.LocalFileHeader? zipHeader = null;
            long zipHeaderBytes = 0;
            if (isPkzip)
            {
                zipHeader = Deserializers.PKZIP.ParseLocalFileHeader(data);
                zipHeaderBytes = data.Position - current;

                // Always trust the PKZIP CRC-32 value over what is supplied
                if (zipHeader != null)
                    expectedCrc = zipHeader.CRC32;

                // If the filename is [NULL], replace with the zip filename
                if (zipHeader?.FileName != null)
                {
                    filename = filename.Replace("[NULL]", zipHeader.FileName);
                    if (includeDebug) Console.WriteLine($"Replaced [NULL] with {zipHeader.FileName}");
                }

                // Debug output
                if (includeDebug) Console.WriteLine($"PKZIP Filename: {zipHeader?.FileName}, PKZIP Expected Read: {zipHeader?.CompressedSize}, PKZIP Expected Write: {zipHeader?.UncompressedSize}, PKZIP Expected CRC-32: {zipHeader?.CRC32:X4}");
            }

            // Set the name from the zip header if missing
            filename ??= zipHeader?.FileName ?? Guid.NewGuid().ToString();

            // Extract the file
            extracted = new MemoryStream();
            if (!Inflate(data, extracted, out long bytesRead, out long bytesWritten, out uint writtenCrc))
            {
                if (includeDebug) Console.Error.WriteLine($"Could not extract {filename}");
                return WiseExtractStatus.FAIL;
            }

            // If not PKZIP, read the checksum bytes
            if (!isPkzip)
            {
                // Seek to the true end of the data
                data.Seek(current + bytesRead, SeekOrigin.Begin);

                // If the read value is off-by-one after checksum
                if (bytesRead == expectedBytesRead - 5)
                {
                    // If not at the end of the file, get the corrected offset
                    if (data.Position + 5 < data.Length)
                    {
                        // TODO: What does this byte represent?
                        byte padding = data.ReadByteValue();
                        bytesRead += 1;

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
                if (data.Position + 4 < data.Length)
                {
                    deflateCrc = data.ReadUInt32LittleEndian();
                    bytesRead += 4;
                }
                // Otherwise, read what is possible and pad with 0x00
                else
                {
                    byte[] deflateCrcBytes = new byte[4];
                    int realCrcLength = data.Read(deflateCrcBytes, 0, (int)(data.Length - data.Position));

                    // Parse as a little-endian 32-bit value
                    deflateCrc = (uint)(deflateCrcBytes[0]
                               | (deflateCrcBytes[1] << 8)
                               | (deflateCrcBytes[2] << 16)
                               | (deflateCrcBytes[3] << 24));

                    bytesRead += realCrcLength;
                }

                // If the CRC to check isn't set
                if (expectedCrc == 0)
                    expectedCrc = deflateCrc;

                // Debug output
                if (includeDebug) Console.WriteLine($"DeflateStream CRC-32: {deflateCrc:X8}");
            }

            // Otherwise, account for the header bytes read
            else
            {
                bytesRead += zipHeaderBytes;
                data.Seek(current + bytesRead, SeekOrigin.Begin);
            }

            // Debug output
            if (includeDebug) Console.WriteLine($"Actual Read: {bytesRead}, Actual Write: {bytesWritten}, Actual CRC-32: {writtenCrc:X8}");

            // If there's a mismatch during both reading and writing
            if (expectedBytesRead >= 0 && expectedBytesRead != bytesRead)
            {
                // This in/out check helps catch false positives, such as
                // files that have an off-by-one mismatch for read values
                // but properly match the output written values.

                // If the written bytes not correct as well
                if (expectedBytesWritten >= 0 && expectedBytesWritten != bytesWritten)
                {
                    // Null the output stream
                    extracted = null;

                    if (includeDebug) Console.Error.WriteLine($"Mismatched read/write values for {filename}!");
                    data.Seek(current, SeekOrigin.Begin);
                    return WiseExtractStatus.WRONG_SIZE;
                }

                // If the written bytes are not being verified
                else if (expectedBytesWritten < 0)
                {
                    // Null the output stream
                    extracted = null;

                    if (includeDebug) Console.Error.WriteLine($"Mismatched read/write values for {filename}!");
                    data.Seek(current, SeekOrigin.Begin);
                    return WiseExtractStatus.WRONG_SIZE;
                }
            }

            // If there's just a mismatch during only writing
            if (expectedBytesRead >= 0 && expectedBytesRead == bytesRead)
            {
                // We want to log this but ignore the error
                if (expectedBytesWritten >= 0 && expectedBytesWritten != bytesWritten)
                {
                    if (includeDebug) Console.WriteLine($"Ignoring mismatched write values for {filename} because read values match!");
                }
            }

            // Otherwise, the write size should be checked normally
            else if (expectedBytesRead == 0 && expectedBytesWritten >= 0 && expectedBytesWritten != bytesWritten)
            {
                // Null the output stream
                extracted = null;

                if (includeDebug) Console.Error.WriteLine($"Mismatched write values for {filename}!");
                data.Seek(current, SeekOrigin.Begin);
                return WiseExtractStatus.WRONG_SIZE;
            }

            // If there's a mismatch with the CRC-32
            if (expectedCrc != 0 && expectedCrc != writtenCrc)
            {
                // Null the output stream
                extracted = null;

                if (includeDebug) Console.Error.WriteLine($"Mismatched CRC-32 values for {filename}!");
                data.Seek(current, SeekOrigin.Begin);
                return WiseExtractStatus.BAD_CRC;
            }

            return WiseExtractStatus.GOOD;
        }

        /// <summary>
        /// Attempt to extract a file defined by a file header
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="dataStart">Start of the deflated data</param>
        /// <param name="obj">Deflate information</param>
        /// <param name="index">File index for automatic naming</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="isPkzip">Indicates if PKZIP containers are used</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted successfully, false otherwise</returns>
        public WiseExtractStatus ExtractFile(Stream data,
            long dataStart,
            InstallFile obj,
            int index,
            string outputDirectory,
            bool isPkzip,
            bool includeDebug)
        {
            // Get expected values
            long expectedBytesRead = obj.DeflateEnd - obj.DeflateStart;
            long expectedBytesWritten = obj.InflatedSize;
            uint expectedCrc = obj.Crc32;

            // Perform path replacements
            string filename = obj.DestinationPathname ?? $"WISE{index:X4}";
            filename = filename.Replace("%", string.Empty);
            data.Seek(dataStart + obj.DeflateStart, SeekOrigin.Begin);
            return ExtractFile(data,
                filename,
                outputDirectory,
                expectedBytesRead,
                expectedBytesWritten,
                expectedCrc,
                isPkzip,
                includeDebug);
        }

        /// <summary>
        /// Attempt to extract a file defined by a file header
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="dataStart">Start of the deflated data</param>
        /// <param name="obj">Deflate information</param>
        /// <param name="index">File index for automatic naming</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="isPkzip">Indicates if PKZIP containers are used</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted successfully, false otherwise</returns>
        public WiseExtractStatus ExtractFile(Stream data,
            long dataStart,
            Unknown0x06 obj,
            int index,
            string outputDirectory,
            bool isPkzip,
            bool includeDebug)
        {
            // Get the generated base name
            string baseName = $"WISE_0x06_{obj.Operand_1:X4}";

            // If there are no deflate objects
            if (obj.DeflateInfo?.Info == null)
            {
                if (includeDebug) Console.WriteLine($"Skipping {baseName} because the deflate object array is null!");
                return WiseExtractStatus.FAIL;
            }

            // Loop through the values
            for (int i = 0; i < obj.DeflateInfo.Info.Length; i++)
            {
                // Get the deflate info object
                var info = obj.DeflateInfo.Info[i];

                // Get expected values
                long expectedBytesRead = info.DeflateEnd - info.DeflateStart;
                long expectedBytesWritten = info.InflatedSize;

                // Perform path replacements
                string filename = $"{baseName}{index:X4}";
                data.Seek(dataStart + info.DeflateStart, SeekOrigin.Begin);
                _ = ExtractFile(data, filename, outputDirectory, expectedBytesRead, expectedBytesWritten, expectedCrc: 0, isPkzip, includeDebug);
            }

            // Always return good -- TODO: Fix this
            return WiseExtractStatus.GOOD;
        }

        /// <summary>
        /// Attempt to extract a file defined by a file header
        /// </summary>
        /// <param name="data">Stream representing the Wise installer</param>
        /// <param name="dataStart">Start of the deflated data</param>
        /// <param name="obj">Deflate information</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="isPkzip">Indicates if PKZIP containers are used</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if the file extracted successfully, false otherwise</returns>
        public WiseExtractStatus ExtractFile(Stream data,
            long dataStart,
            CustomDialogSet obj,
            string outputDirectory,
            bool isPkzip,
            bool includeDebug)
        {
            // Get expected values
            long expectedBytesRead = obj.DeflateEnd - obj.DeflateStart;
            long expectedBytesWritten = obj.InflatedSize;

            // Perform path replacements
            string filename = $"WISE_0x14_{obj.DisplayVariable}-{obj.Name}";
            filename = filename.Replace("%", string.Empty);
            data.Seek(dataStart + obj.DeflateStart, SeekOrigin.Begin);
            return ExtractFile(data, filename, outputDirectory, expectedBytesRead, expectedBytesWritten, expectedCrc: 0, isPkzip, includeDebug);
        }

        /// <summary>
        /// Inflate an input stream to an output file path
        /// </summary>
        private static bool Inflate(Stream input,
            string outputPath,
            out long inputSize,
            out long outputSize,
            out uint crc)
        {
            var output = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            return Inflate(input, output, out inputSize, out outputSize, out crc);
        }

        /// <summary>
        /// Inflate an input stream to an output stream
        /// </summary>
        private static bool Inflate(Stream input,
            Stream output,
            out long inputSize,
            out long outputSize,
            out uint crc)
        {
            inputSize = 0;
            outputSize = 0;
            crc = 0;

            var hasher = new HashWrapper(HashType.CRC32);
            try
            {
                long start = input.Position;
                var ds = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true);
                while (true)
                {
                    byte[] buf = new byte[1024];
                    int read = ds.Read(buf, 0, buf.Length);
                    if (read == 0)
                        break;

                    hasher.Process(buf, 0, read);
                    output.Write(buf, 0, read);
                }

                // Save the deflate values
                inputSize = ds.TotalIn;
                outputSize = ds.TotalOut;
            }
            catch
            {
                return false;
            }

            hasher.Terminate();
            byte[] hashBytes = hasher.CurrentHashBytes!;
            crc = BitConverter.ToUInt32(hashBytes, 0);
            return true;
        }

        #endregion
    }
}
