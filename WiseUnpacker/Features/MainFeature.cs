using System;
using System.IO;
using SabreTools.CommandLine;
using SabreTools.CommandLine.Inputs;
using SabreTools.Hashing;
using SabreTools.IO.Compression.Deflate;
using SabreTools.IO.Extensions;
using SabreTools.Serialization;
using SabreTools.Serialization.Wrappers;

namespace WiseUnpacker.Features
{
    internal sealed class MainFeature : Feature
    {
        #region Feature Definition

        public const string DisplayName = "main";

        /// <remarks>Flags are unused</remarks>
        private static readonly string[] _flags = [];

        /// <remarks>Description is unused</remarks>
        private const string _description = "";

        #endregion

        #region Inputs

        private const string _debugName = "debug";
        internal readonly FlagInput DebugInput = new(_debugName, ["-d", "--debug"], "Enable debug mode");

        private const string _extractName = "extract";
        internal readonly FlagInput ExtractInput = new(_extractName, ["-x", "--extract"], "Extract files (default if nothing else provided)");

        private const string _fileOnlyName = "file-only";
        internal readonly FlagInput FileOnlyInput = new(_fileOnlyName, ["-f", "--file"], "Print to file only");

        private const string _infoName = "info";
        internal readonly FlagInput InfoInput = new(_infoName, ["-i", "--info"], "Print overlay and script info");

#if NETCOREAPP
        private const string _jsonName = "json";
        internal readonly FlagInput JsonInput = new(_jsonName, ["-j", "--json"], "Print info as JSON");
#endif

        private const string _outputPathName = "output-path";
        internal readonly StringInput OutputPathInput = new(_outputPathName, ["-o", "--outdir"], "Set output path for extraction (required)");

        private const string _perFileName = "per-file";
        internal readonly FlagInput PerFileInput = new(_perFileName, ["-p", "--per-file"], "Print per-file statistics");

        #endregion

        #region Properties

        /// <summary>
        /// Enable debug output for relevant operations
        /// </summary>
        public bool Debug { get; private set; }

        /// <summary>
        /// Enable extraction for the input file
        /// </summary>
        public bool Extract { get; private set; }

        /// <summary>
        /// Output information to file only, skip printing to console
        /// </summary>
        public bool FileOnly { get; private set; }

        /// <summary>
        /// Print both the overlay and script information
        /// to screen and file, if possible
        /// </summary>
        public bool Info { get; private set; }

#if NETCOREAPP
        /// <summary>
        /// Enable JSON output
        /// </summary>
        public bool Json { get; private set; }
#endif

        /// <summary>
        /// Print per-file statistics information
        /// </summary>
        public bool PerFile { get; private set; }

        /// <summary>
        /// Output path for archive extraction
        /// </summary>
        public string OutputPath { get; private set; } = string.Empty;

        #endregion

        #region Instance Variables

        /// <summary>
        /// Statistics for tracking
        /// </summary>
        private static readonly Statistics _statistics = new();

        #endregion

        public MainFeature()
            : base(DisplayName, _flags, _description)
        {
            RequiresInputs = true;

            Add(DebugInput);

#if NETCOREAPP
            InfoInput.Add(JsonInput);
#endif
            InfoInput.Add(FileOnlyInput);
            InfoInput.Add(PerFileInput);
            Add(InfoInput);

            Add(ExtractInput);
            Add(OutputPathInput);
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            // Get the options from the arguments
            Debug = GetBoolean(_debugName);
            Extract = GetBoolean(_extractName);
            FileOnly = GetBoolean(_fileOnlyName);
            Info = GetBoolean(_infoName);
#if NETCOREAPP
            Json = GetBoolean(_jsonName);
#endif
            OutputPath = GetString(_outputPathName) ?? string.Empty;

            // If neither info nor extract is defined, default to extract
            if (!Info && !Extract)
                Extract = true;

            // Validate the output path
            if (!ValidateExtractionPath())
                return false;

            // Loop through the input paths
            for (int i = 0; i < Inputs.Count; i++)
            {
                string arg = Inputs[i];
                ProcessPath(arg);
            }

            // Export statistics
            if (Info)
                _statistics.ExportStatistics();

            return true;
        }

        /// <inheritdoc/>
        public override bool VerifyInputs() => Inputs.Count > 0;

        /// <summary>
        /// Wrapper to process a single path
        /// </summary>
        /// <param name="path">File or directory path</param>
        private void ProcessPath(string path)
        {
            // Normalize by getting the full path
            path = Path.GetFullPath(path);
            Console.WriteLine($"Checking possible path: {path}");

            // Check if the file or directory exists
            if (File.Exists(path))
            {
                ProcessFile(path);
            }
            else if (Directory.Exists(path))
            {
                foreach (string file in path.SafeEnumerateFiles("*", SearchOption.AllDirectories))
                {
                    ProcessFile(file);
                }
            }
            else
            {
                Console.WriteLine($"{path} does not exist, skipping...");
            }
        }

        /// <summary>
        /// Wrapper to process a single file
        /// </summary>
        /// <param name="file">File path</param>
        private void ProcessFile(string file)
        {
            // Attempt to print information
            if (Info)
                PrintFileInfo(file);

            // Attempt to extract the file
            if (Extract)
                ExtractFile(file);
        }

        /// <summary>
        /// Validate the extraction path
        /// </summary>
        private bool ValidateExtractionPath()
        {
            // Null or empty output path
            if (string.IsNullOrEmpty(OutputPath))
            {
                Console.WriteLine("Output directory required for extraction!");
                Console.WriteLine();
                return false;
            }

            // Malformed output path or invalid location
            try
            {
                OutputPath = Path.GetFullPath(OutputPath);
                Directory.CreateDirectory(OutputPath);
            }
            catch
            {
                Console.WriteLine("Output directory could not be created!");
                Console.WriteLine();
                return false;
            }

            return true;
        }

        #region Info

        /// <summary>
        /// Wrapper to print overlay and script information for a single file
        /// </summary>
        /// <param name="file">File path</param>
        private void PrintFileInfo(string file)
        {
            // Get the base info output name
            string filenameBase = $"{file}-{DateTime.Now:yyyy-MM-dd_HHmmss.ffff}";
            if (!string.IsNullOrEmpty(OutputPath))
                filenameBase = Path.Combine(OutputPath, Path.GetFileName(filenameBase));

            // Ensure the directory is created
            string? tempDirectory = Path.GetDirectoryName(filenameBase);
            if (tempDirectory != null)
                Directory.CreateDirectory(tempDirectory);

            // Ensure the statistics object is created
            if (!_statistics.FilesMap.ContainsKey(file))
                _statistics.FilesMap[file] = new();

            var fileStatistics = _statistics.FilesMap[file];

            Console.WriteLine($"Attempting to print info for {file}");

            try
            {
                using Stream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // Read the stream as an executable
                var wrapper = WrapperFactory.CreateExecutableWrapper(stream);

                // Figure out if we have an overlay or section
                long overlayOffset = -1;
                WiseSectionHeader? wiseSection = null;
                if (wrapper is PortableExecutable pex)
                {
                    overlayOffset = pex.FindWiseOverlayHeader();
                    wiseSection = pex.WiseSection;
                }
                else if (wrapper is NewExecutable nex)
                {
                    overlayOffset = nex.FindWiseOverlayHeader();
                }
                else
                {
                    _statistics.AddInvalidPath(file);
                    return;
                }

                // Overlay headers take precedence
                if (overlayOffset >= 0 && overlayOffset < stream.Length)
                {
                    stream.Seek(overlayOffset, SeekOrigin.Begin);
                    var overlayHeader = WiseOverlayHeader.Create(stream);
                    if (overlayHeader == null)
                    {
                        _statistics.AddErroredPath(file);
                        return;
                    }

                    // Process header statistics and print
                    _statistics.ProcessStatistics(file, overlayHeader);
                    PrintOverlayHeader(filenameBase, overlayHeader);

                    // Process header-defined files
                    var scriptStream = ProcessHeaderDefinedFiles(stream, fileStatistics, overlayHeader);
                    if (scriptStream == null)
                    {
                        if (PerFile)
                            PrintOverlayHeaderStats(filenameBase, fileStatistics);

                        return;
                    }

                    // Output overlay header stats with WISE0001.DLL hashes
                    if (PerFile)
                        PrintOverlayHeaderStats(filenameBase, fileStatistics);

                    // Try to parse the script information
                    scriptStream?.Seek(0, SeekOrigin.Begin);
                    var script = WiseScript.Create(scriptStream);
                    if (script == null)
                    {
                        _statistics.AddErroredPath(file);
                        Console.WriteLine($"No valid script could be extracted from {file}, skipping...");
                        return;
                    }

                    // Process script statistics and print
                    _statistics.ProcessStatistics(file, script);
                    PrintScript(filenameBase, script);
                    if (PerFile)
                        PrintScriptStatistics(filenameBase, fileStatistics);
                }

                // Section headers are checked after
                if (wiseSection != null)
                {
                    // Process header statistics and print
                    // TODO: Add statistics for the section header somewhere
                    //_statistics.ProcessStatistics(file, sectionHeader);
                    PrintSectionHeader(filenameBase, wiseSection);
                }
            }
            catch (Exception ex)
            {
                _statistics.AddErroredPath(file);
                Console.WriteLine(Debug ? ex : "[Exception opening file, please try again]");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Process header-defined files
        /// </summary>
        /// <param name="stream">Stream that represents the extractable data</param>
        /// <param name="options">User-defined options</param>
        /// <param name="fileStatistics">Per-file statistics to add hashes to</param>
        /// <param name="header">Overlay header to pull information from</param>
        /// <returns>Populated stream representing the script on success, null otherwise</returns>
        private MemoryStream? ProcessHeaderDefinedFiles(Stream stream, PerFileStatistics fileStatistics, WiseOverlayHeader header)
        {
            #region WiseColors.dib

            var expected = new DeflateInfo
            {
                InputSize = header.DibDeflatedSize,
                OutputSize = header.DibInflatedSize,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[0] = tempHash;
                }
            }

            #endregion

            #region WiseScript.bin

            // Create the expected script output information
            expected = new DeflateInfo
            {
                InputSize = header.WiseScriptDeflatedSize,
                OutputSize = header.WiseScriptInflatedSize,
                Crc32 = 0,
            };

            // Try to extract the script
            var scriptStream = new MemoryStream();
            var scriptResult = InflateWrapper.ExtractStream(stream, scriptStream, expected, header.IsPKZIP, Debug, out _);
            if (scriptResult == ExtractionStatus.FAIL)
            {
                return null;
            }
            else if (scriptResult == ExtractionStatus.GOOD)
            {
                scriptStream.Seek(0, SeekOrigin.Begin);
                string? tempHash = HashTool.GetStreamHash(scriptStream, HashType.SHA1, leaveOpen: true);
                if (tempHash != null)
                    fileStatistics.HeaderDefinedFileHashes[1] = tempHash;

                scriptStream.Seek(0, SeekOrigin.Begin);
            }

            #endregion

            #region WISE0001.DLL

            expected = new DeflateInfo
            {
                InputSize = header.WiseDllDeflatedSize,
                OutputSize = -1,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[2] = tempHash;
                }
            }

            #endregion

            #region CTL3D32.DLL

            expected = new DeflateInfo
            {
                InputSize = header.Ctl3d32DeflatedSize,
                OutputSize = -1,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[3] = tempHash;
                }
            }

            #endregion

            #region FILE0004

            expected = new DeflateInfo
            {
                InputSize = header.SomeData4DeflatedSize,
                OutputSize = -1,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[4] = tempHash;
                }
            }

            #endregion

            #region Ocxreg32.EXE

            expected = new DeflateInfo
            {
                InputSize = header.RegToolDeflatedSize,
                OutputSize = -1,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[5] = tempHash;
                }
            }

            #endregion

            #region PROGRESS.DLL

            expected = new DeflateInfo
            {
                InputSize = header.ProgressDllDeflatedSize,
                OutputSize = -1,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[6] = tempHash;
                }
            }

            #endregion

            #region FILE0007

            expected = new DeflateInfo
            {
                InputSize = header.SomeData7DeflatedSize,
                OutputSize = -1,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[7] = tempHash;
                }
            }

            #endregion

            #region FILE0008

            expected = new DeflateInfo
            {
                InputSize = header.SomeData8DeflatedSize,
                OutputSize = -1,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[8] = tempHash;
                }
            }

            #endregion

            #region FILE0009

            expected = new DeflateInfo
            {
                InputSize = header.SomeData9DeflatedSize,
                OutputSize = -1,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[9] = tempHash;
                }
            }

            #endregion

            #region FILE000A

            expected = new DeflateInfo
            {
                InputSize = header.SomeData10DeflatedSize,
                OutputSize = -1,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[10] = tempHash;
                }
            }

            #endregion

            #region INSTALL_SCRIPT

            expected = new DeflateInfo
            {
                InputSize = header.InstallScriptDeflatedSize,
                OutputSize = -1,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[11] = tempHash;
                }
            }

            #endregion

            #region FILE0XX.DAT

            expected = new DeflateInfo
            {
                InputSize = header.FinalFileDeflatedSize,
                OutputSize = header.FinalFileInflatedSize,
                Crc32 = 0,
            };

            using (var tempStream = new MemoryStream())
            {
                if (InflateWrapper.ExtractStream(stream, tempStream, expected, header.IsPKZIP, Debug, out _) == ExtractionStatus.GOOD)
                {
                    tempStream.Seek(0, SeekOrigin.Begin);
                    string? tempHash = HashTool.GetStreamHash(tempStream, HashType.SHA1, leaveOpen: true);
                    if (tempHash != null)
                        fileStatistics.HeaderDefinedFileHashes[12] = tempHash;
                }
            }

            #endregion

            return scriptStream;
        }

        /// <summary>
        /// Print overlay header information, if possible
        /// </summary>
        /// <param name="filenameBase">Base filename pattern to use for output</param>
        /// <param name="header">Overlay header to pull information from</param>
        private void PrintOverlayHeader(string filenameBase, WiseOverlayHeader header)
        {
#if NETCOREAPP
            // If we have the JSON flag
            if (Json)
            {
                // Create the output data
                string serializedData = header.ExportJSON();

                // Write the output data
                using var jsw = new StreamWriter(File.OpenWrite($"{filenameBase}-overlay.json"));
                jsw.WriteLine(serializedData);
                jsw.Flush();
            }
#endif

            // Create the header output data
            var builder = header.ExportStringBuilder();
            if (builder == null)
            {
                Console.WriteLine("No header information could be generated");
                return;
            }

            // Only print to console if enabled
            if (!FileOnly)
                Console.WriteLine(builder);

            using var sw = new StreamWriter(File.OpenWrite($"{filenameBase}-overlay.txt"));
            sw.WriteLine(builder.ToString());
            sw.Flush();
        }

        /// <summary>
        /// Print overlay header statistical information, if possible
        /// </summary>
        /// <param name="filenameBase">Base filename pattern to use for output</param>
        /// <param name="statistics">Statistics representing the file</param>
        private void PrintOverlayHeaderStats(string filenameBase, PerFileStatistics statistics)
        {
#if NETCOREAPP
            // If we have the JSON flag
            if (Json)
            {
                // TODO: Not implemented
            }
#endif

            // Create stats output data
            using var sw = new StreamWriter(File.OpenWrite($"{filenameBase}-overlay-stats.txt"));
            statistics.ExportOverlayHeaderStatistics(sw);
            sw.Flush();
        }

        /// <summary>
        /// Print script information, if possible
        /// </summary>
        /// <param name="filenameBase">Base filename pattern to use for output</param>
        /// <param name="script">Script to print information for</param>
        private void PrintScript(string filenameBase, WiseScript script)
        {
#if NETCOREAPP
            // If we have the JSON flag
            if (Json)
            {
                // Create the output data
                string serializedData = script.ExportJSON();

                // Write the output data
                using var jsw = new StreamWriter(File.OpenWrite($"{filenameBase}-script.json"));
                jsw.WriteLine(serializedData);
                jsw.Flush();
            }
#endif

            // Create script output data
            var builder = script.ExportStringBuilder();
            if (builder == null)
            {
                Console.WriteLine("No header information could be generated");
                return;
            }

            using var sw = new StreamWriter(File.OpenWrite($"{filenameBase}-script.txt"));
            sw.WriteLine(builder.ToString());
            sw.Flush();
        }

        /// <summary>
        /// Print script statistical information, if possible
        /// </summary>
        /// <param name="filenameBase">Base filename pattern to use for output</param>
        /// <param name="statistics">Statistics representing the file</param>
        private void PrintScriptStatistics(string filenameBase, PerFileStatistics statistics)
        {
#if NETCOREAPP
            // If we have the JSON flag
            if (Json)
            {
                // TODO: Not implemented
            }
#endif

            // Create stats output data
            using var sw = new StreamWriter(File.OpenWrite($"{filenameBase}-script-stats.txt"));
            statistics.ExportScriptStatistics(sw);
            sw.Flush();
        }

        /// <summary>
        /// Print section header information, if possible
        /// </summary>
        /// <param name="filenameBase">Base filename pattern to use for output</param>
        /// <param name="header">Overlay header to pull information from</param>
        private void PrintSectionHeader(string filenameBase, WiseSectionHeader header)
        {
#if NETCOREAPP
            // If we have the JSON flag
            if (Json)
            {
                // Create the output data
                string serializedData = header.ExportJSON();

                // Write the output data
                using var jsw = new StreamWriter(File.OpenWrite($"{filenameBase}-overlay.json"));
                jsw.WriteLine(serializedData);
                jsw.Flush();
            }
#endif

            // Create the header output data
            var builder = header.ExportStringBuilder();
            if (builder == null)
            {
                Console.WriteLine("No header information could be generated");
                return;
            }

            // Only print to console if enabled
            if (!FileOnly)
                Console.WriteLine(builder);

            using var sw = new StreamWriter(File.OpenWrite($"{filenameBase}-overlay.txt"));
            sw.WriteLine(builder.ToString());
            sw.Flush();
        }

        #endregion

        #region Extract

        /// <summary>
        /// Wrapper to extract all files from the input
        /// </summary>
        /// <param name="file">File path</param>
        private void ExtractFile(string file)
        {
            using Stream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Read the stream as an executable
            var wrapper = WrapperFactory.CreateExecutableWrapper(stream);

            // Extract based on the executable
            if (wrapper is PortableExecutable pex)
            {
                if (pex.ExtractWiseOverlay(OutputPath, Debug))
                {
                    Console.WriteLine($"Extracted {file} to {OutputPath}");
                }
                else if (pex.ExtractWiseSection(OutputPath, Debug))
                {
                    Console.WriteLine($"Extracted {file} to {OutputPath}");
                }
                else
                {
                    Console.WriteLine(value: $"Failed to extract {file}!");
                    _statistics.AddFailedExtractPath(file);
                }
            }
            else if (wrapper is NewExecutable nex)
            {
                if (nex.ExtractWise(OutputPath, Debug))
                {
                    Console.WriteLine($"Extracted {file} to {OutputPath}");
                }
                else
                {
                    Console.WriteLine(value: $"Failed to extract {file}!");
                    _statistics.AddFailedExtractPath(file);
                }
            }
            else
            {
                _statistics.AddInvalidPath(file);
                return;
            }
        }

        #endregion
    }
}
