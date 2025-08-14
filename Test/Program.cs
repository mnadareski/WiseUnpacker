using System;
using System.IO;
using SabreTools.Hashing;
using SabreTools.IO.Compression.Deflate;
using SabreTools.IO.Extensions;
using SabreTools.Serialization;
using SabreTools.Serialization.Wrappers;

namespace Test
{
    class Program
    {
        /// <summary>
        /// Statistics for tracking
        /// </summary>
        private static readonly Statistics _statistics = new();

        static void Main(string[] args)
        {
            // Get the options from the arguments
            var options = Options.ParseOptions(args);

            // If we have an invalid state
            if (options == null)
            {
                Options.DisplayHelp();
                return;
            }

            // Loop through the input paths
            foreach (string inputPath in options.InputPaths)
            {
                ProcessPath(inputPath, options);
            }

            // Export statistics
            if (options.Info)
                _statistics.ExportStatistics();
        }

        /// <summary>
        /// Wrapper to process a single path
        /// </summary>
        /// <param name="path">File or directory path</param>
        /// <param name="options">User-defined options</param>
        private static void ProcessPath(string path, Options options)
        {
            // Normalize by getting the full path
            path = Path.GetFullPath(path);
            Console.WriteLine($"Checking possible path: {path}");

            // Check if the file or directory exists
            if (File.Exists(path))
            {
                ProcessFile(path, options);
            }
            else if (Directory.Exists(path))
            {
                foreach (string file in IOExtensions.SafeEnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    ProcessFile(file, options);
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
        /// <param name="options">User-defined options</param>
        private static void ProcessFile(string file, Options options)
        {
            // Attempt to print information
            if (options.Info)
                PrintFileInfo(file, options.OutputPath, options);

            // Attempt to extract the file
            if (options.Extract)
                ExtractFile(file, options.OutputPath, options.Debug);
        }

        #region Info

        /// <summary>
        /// Wrapper to print overlay and script information for a single file
        /// </summary>
        /// <param name="file">File path</param>
        /// <param name="outputDirectory">Output directory path</param>
        /// <param name="options">User-defined options</param>
        private static void PrintFileInfo(string file, string outputDirectory, Options options)
        {
            // Get the base info output name
            string filenameBase = $"{file}-{DateTime.Now:yyyy-MM-dd_HHmmss.ffff}";
            if (!string.IsNullOrEmpty(outputDirectory))
                filenameBase = Path.Combine(outputDirectory, Path.GetFileName(filenameBase));

            // Ensure the directory is created
            string? tempDirectory = Path.GetDirectoryName(filenameBase);
            if (tempDirectory != null)
                Directory.CreateDirectory(tempDirectory);

            // Ensure the statistics object is created
            if (!_statistics.PerFileStatistics.ContainsKey(file))
                _statistics.PerFileStatistics[file] = new();

            var fileStatistics = _statistics.PerFileStatistics[file];

            Console.WriteLine($"Attempting to print info for {file}");

            try
            {
                using Stream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // Try to find the overlay header
                if (!WiseOverlayHeader.FindOverlayHeader(stream, options.Debug, out var header) || header == null)
                {
                    _statistics.AddInvalidPath(file);
                    Console.WriteLine($"No valid header could be found in {file}, skipping...");
                    return;
                }

                // Process header statistics and print
                _statistics.ProcessStatistics(file, header);
                PrintOverlayHeader(filenameBase, header, options);

                // Seek to the script offset
                stream.Seek(header.DibDeflatedSize, SeekOrigin.Current);

                // Create the expected script output information
                var expectedScript = new DeflateInfo
                {
                    InputSize = header.WiseScriptDeflatedSize,
                    OutputSize = header.WiseScriptInflatedSize,
                    Crc32 = 0,
                };

                // Try to extract the script
                var scriptStream = new MemoryStream();
                if (InflateWrapper.ExtractStream(stream, scriptStream, expectedScript, header.IsPKZIP, options.Debug, out _) == ExtractionStatus.FAIL)
                {
                    if (options.PerFile)
                        PrintOverlayHeaderStats(filenameBase, fileStatistics, options);

                    return;
                }

                // Create the expected script output information
                var expectedDll = new DeflateInfo
                {
                    InputSize = header.WiseDllDeflatedSize,
                    OutputSize = -1,
                    Crc32 = 0,
                };

                // Try to extract WISE0001.DLL
                using (var dllStream = new MemoryStream())
                {
                    if (InflateWrapper.ExtractStream(stream, dllStream, expectedDll, header.IsPKZIP, options.Debug, out _) == ExtractionStatus.GOOD)
                    {
                        dllStream.Seek(0, SeekOrigin.Begin);
                        string? dllHash = HashTool.GetStreamHash(dllStream, HashType.SHA1, leaveOpen: true);
                        if (dllHash != null)
                            fileStatistics.WiseDllHash = dllHash;
                    }
                }

                // Output overlay header stats with WISE0001.DLL hashes
                if (options.PerFile)
                    PrintOverlayHeaderStats(filenameBase, fileStatistics, options);

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
                PrintScript(filenameBase, script, options);
                if (options.PerFile)
                    PrintScriptStatistics(filenameBase, fileStatistics, options);
            }
            catch (Exception ex)
            {
                _statistics.AddErroredPath(file);
                Console.WriteLine(options.Debug ? ex : "[Exception opening file, please try again]");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Print overlay header information, if possible
        /// </summary>
        /// <param name="filenameBase">Base filename pattern to use for output</param>
        /// <param name="options">User-defined options</param>
        private static void PrintOverlayHeader(string filenameBase, WiseOverlayHeader header, Options options)
        {
#if NETCOREAPP
            // If we have the JSON flag
            if (options.Json)
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
            var builder = header.ExportStringBuilderExt();
            if (builder == null)
            {
                Console.WriteLine("No header information could be generated");
                return;
            }

            // Only print to console if enabled
            if (!options.FileOnly)
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
        /// <param name="options">User-defined options</param>
        private static void PrintOverlayHeaderStats(string filenameBase, PerFileStatistics statistics, Options options)
        {
#if NETCOREAPP
            // If we have the JSON flag
            if (options.Json)
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
        /// <param name="options">User-defined options</param>
        private static void PrintScript(string filenameBase, WiseScript script, Options options)
        {
#if NETCOREAPP
            // If we have the JSON flag
            if (options.Json)
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
            var builder = script.ExportStringBuilderExt();
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
        /// <param name="options">User-defined options</param>
        private static void PrintScriptStatistics(string filenameBase, PerFileStatistics statistics, Options options)
        {
#if NETCOREAPP
            // If we have the JSON flag
            if (options.Json)
            {
                // TODO: Not implemented
            }
#endif

            // Create stats output data
            using var sw = new StreamWriter(File.OpenWrite($"{filenameBase}-script-stats.txt"));
            statistics.ExportScriptStatistics(sw);
            sw.Flush();
        }

        #endregion

        #region Extract

        /// <summary>
        /// Wrapper to extract all files from the input
        /// </summary>
        /// <param name="file">File path</param>
        /// <param name="outputDirectory">Output directory path</param>
        /// <param name="includeDebug">Enable including debug information</param>
        private static void ExtractFile(string file, string outputDirectory, bool includeDebug)
        {
            if (WiseOverlayHeader.ExtractAll(file, outputDirectory, includeDebug))
            {
                Console.WriteLine($"Extracted {file} to {outputDirectory}");
            }
            else
            {
                Console.WriteLine(value: $"Failed to extract {file}!");
                _statistics.AddFailedExtractPath(file);
            }
        }

        #endregion
    }
}
