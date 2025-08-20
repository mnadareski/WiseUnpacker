using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        /// <inheritdoc cref="ScriptHeader.DateTime"/>
        public uint DateTime => Model.Header?.DateTime ?? 0;

        /// <inheritdoc cref="ScriptHeader.Flags"/>
        public ushort Flags => Model.Header?.Flags ?? 0;

        /// <inheritdoc cref="ScriptHeader.FontSize"/>
        public uint FontSize => Model.Header?.FontSize ?? 0;

        /// <inheritdoc cref="ScriptHeader.FTPURL"/>
        public string? FTPURL => Model.Header?.FTPURL;

        /// <inheritdoc cref="ScriptFile.HeaderStrings"/>
        public string[]? HeaderStrings => Model.Header?.HeaderStrings;

        /// <inheritdoc cref="ScriptHeader.LanguageCount"/>
        public byte LanguageCount => Model.Header?.LanguageCount ?? 0;

        /// <inheritdoc cref="ScriptHeader.LogPathname"/>
        public string? LogPathname => Model.Header?.LogPathname;

        /// <inheritdoc cref="ScriptHeader.MessageFont"/>
        public string? MessageFont => Model.Header?.MessageFont;

        /// <inheritdoc cref="ScriptFile.States"/>
        public MachineState[]? States => Model.States;

        /// <inheritdoc cref="ScriptHeader.UnknownU16_1"/>
        public ushort UnknownU16_1 => Model.Header?.UnknownU16_1 ?? 0;

        /// <inheritdoc cref="ScriptHeader.UnknownU16_2"/>
        public ushort UnknownU16_2 => Model.Header?.UnknownU16_2 ?? 0;

        /// <inheritdoc cref="ScriptFile.VariableLengthData"/>
        public byte[]? VariableLengthData => Model.Header?.VariableLengthData;

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
                var model = Deserializers.WiseScript.DeserializeStream(data);
                if (model == null)
                    return null;

                return new WiseScript(model, data);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Processing

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

                        // Try to write to the output directory
                        WriteIniData(editIniFile, outputDirectory, ++normalFileCount);
                        break;

                    case OperationCode.DisplayBillboard:
                        if (state.Data is not DisplayBillboard displayBillboard)
                            return false;

                        // Try to extract to the output directory
                        ExtractFile(data, dataStart, displayBillboard, ++normalFileCount, outputDirectory, isPkzip, includeDebug);
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

        #endregion

        #region Extraction

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
        public static ExtractionStatus ExtractFile(Stream data,
            long dataStart,
            InstallFile obj,
            int index,
            string outputDirectory,
            bool isPkzip,
            bool includeDebug)
        {
            // Get expected values
            var expected = new DeflateInfo
            {
                InputSize = obj.DeflateEnd - obj.DeflateStart,
                OutputSize = obj.InflatedSize,
                Crc32 = obj.Crc32,
            };

            // Perform path replacements
            string filename = obj.DestinationPathname ?? $"WISE{index:X4}";
            filename = filename.Replace("%", string.Empty);
            data.Seek(dataStart + obj.DeflateStart, SeekOrigin.Begin);
            return InflateWrapper.ExtractFile(data,
                filename,
                outputDirectory,
                expected,
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
        public static ExtractionStatus ExtractFile(Stream data,
            long dataStart,
            DisplayBillboard obj,
            int index,
            string outputDirectory,
            bool isPkzip,
            bool includeDebug)
        {
            // Get the generated base name
            string baseName = $"CustomBillboardSet_{obj.Flags:X4}-{obj.Operand_2}-{obj.Operand_3}";

            // If there are no deflate objects
            if (obj.DeflateInfo == null)
            {
                if (includeDebug) Console.WriteLine($"Skipping {baseName} because the deflate object array is null!");
                return ExtractionStatus.FAIL;
            }

            // Loop through the values
            for (int i = 0; i < obj.DeflateInfo.Length; i++)
            {
                // Get the deflate info object
                var info = obj.DeflateInfo[i];

                // Get expected values
                var expected = new DeflateInfo
                {
                    InputSize = info.DeflateEnd - info.DeflateStart,
                    OutputSize = info.InflatedSize,
                    Crc32 = 0,
                };

                // Perform path replacements
                string filename = $"{baseName}{index:X4}";
                data.Seek(dataStart + info.DeflateStart, SeekOrigin.Begin);
                _ = InflateWrapper.ExtractFile(data, filename, outputDirectory, expected, isPkzip, includeDebug);
            }

            // Always return good -- TODO: Fix this
            return ExtractionStatus.GOOD;
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
        public static ExtractionStatus ExtractFile(Stream data,
            long dataStart,
            CustomDialogSet obj,
            string outputDirectory,
            bool isPkzip,
            bool includeDebug)
        {
            // Get expected values
            var expected = new DeflateInfo
            {
                InputSize = obj.DeflateEnd - obj.DeflateStart,
                OutputSize = obj.InflatedSize,
                Crc32 = 0,
            };

            // Perform path replacements
            string filename = $"CustomDialogSet_{obj.DisplayVariable}-{obj.Name}";
            filename = filename.Replace("%", string.Empty);
            data.Seek(dataStart + obj.DeflateStart, SeekOrigin.Begin);
            return InflateWrapper.ExtractFile(data, filename, outputDirectory, expected, isPkzip, includeDebug);
        }

        /// <summary>
        /// Attempt to write INI data to the correct file
        /// </summary>
        /// <param name="obj">INI information</param>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="index">File index for automatic naming</param>
        private static void WriteIniData(EditIniFile obj, string outputDirectory, int index)
        {
            // Ensure directory separators are consistent
            string iniFilePath = obj.Pathname ?? $"WISE{index:X4}.ini";
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
                iniFile.Write(Encoding.ASCII.GetBytes($"[{obj.Section}]\n"));
                iniFile.Write(Encoding.ASCII.GetBytes($"{obj.Values ?? string.Empty}\n"));
                iniFile.Flush();
            }
        }

        #endregion
    }
}
