using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.Serialization;
using SabreTools.Serialization.Wrappers;

namespace Test
{
    class Program
    {
        #region Statistics

        /// <summary>
        /// All paths that threw an exception during parsing
        /// </summary>
        private static readonly List<string> _erroredPaths = [];

        /// <summary>
        /// All paths that failed to extract all items
        /// </summary>
        private static readonly List<string> _failedExtractPaths = [];

        /// <summary>
        /// Counts for each of the flags
        /// </summary>
        private static readonly int[] _flagCounts = new int[32];

        /// <summary>
        /// All paths that were marked as invalid
        /// </summary>
        private static readonly List<string> _invalidPaths = [];

        #endregion

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
                ExportStatistics();
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
                PrintFileInfo(file, options.OutputPath, options.Debug);

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
        /// <param name="includeDebug">Enable including debug information</param>
        private static void PrintFileInfo(string file, string outputDirectory, bool includeDebug)
        {
            // Get the base info output name
            string filenameBase = $"{file}-{DateTime.Now:yyyy-MM-dd_HHmmss.ffff}";
            if (!string.IsNullOrEmpty(outputDirectory))
                filenameBase = Path.Combine(outputDirectory, filenameBase);

            Console.WriteLine($"Attempting to print info for {file}");

            try
            {
                using Stream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // Try to find the overlay header
                if (!WiseOverlayHeader.FindOverlayHeader(stream, includeDebug, out var header) || header == null)
                {
                    _invalidPaths.Add(file);
                    Console.WriteLine($"No valid header could be found in {file}, skipping...");
                    return;
                }

                // Process header statistics
                ProcessStatistics(header);

                // Create the header output data
                var hBuilder = header.ExportStringBuilderExt();
                if (hBuilder == null)
                {
                    Console.WriteLine("No header information could be generated");
                    return;
                }

                // Try to read the script information
                stream.Seek(header.DibDeflatedSize, SeekOrigin.Current);
                string scriptFilename = "WiseScript.bin";
                if (header.ExtractStream(stream, ref scriptFilename, header.WiseScriptDeflatedSize, header.WiseScriptInflatedSize, 0, includeDebug, out var extracted) == WiseOverlayHeader.ExtractStatus.FAIL)
                    return;

                // Try to parse the script information
                extracted?.Seek(0, SeekOrigin.Begin);
                var script = WiseScript.Create(extracted);
                if (script == null)
                {
                    _erroredPaths.Add(file);
                    Console.WriteLine($"No valid script could be extracted from {file}, skipping...");
                    return;
                }

                // Create script output data
                var sBuilder = script.ExportStringBuilderExt();
                if (sBuilder == null)
                {
                    Console.WriteLine("No header information could be generated");
                    return;
                }

                Console.WriteLine(hBuilder);
                using var sw = new StreamWriter(File.OpenWrite($"{filenameBase}.txt"));
                sw.WriteLine(hBuilder.ToString());
                sw.WriteLine();
                sw.WriteLine(sBuilder.ToString());
                sw.Flush();
            }
            catch (Exception ex)
            {
                _erroredPaths.Add(file);
                Console.WriteLine(includeDebug ? ex : "[Exception opening file, please try again]");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Process statistics for a WiseOverlayHeader
        /// </summary>
        private static void ProcessStatistics(WiseOverlayHeader header)
        {
            // Flags
            for (int i = 0; i < 32; i++)
            {
                int flags = (int)header.Flags;
                if ((flags & (1 << i)) == (1 << i))
                    _flagCounts[i]++;
            }
        }

        /// <summary>
        /// Write all capture statistics to file
        /// </summary>
        private static void ExportStatistics()
        {
            using var sw = new StreamWriter(File.OpenWrite($"stats-{DateTime.Now:yyyy-MM-dd_HHmmss.ffff}.txt"));

            // Invalid Paths
            if (_invalidPaths.Count > 0)
            {
                sw.WriteLine("Invalid Paths:");
                foreach (string path in _invalidPaths)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Errored Paths
            if (_erroredPaths.Count > 0)
            {
                sw.WriteLine("Errored Paths:");
                foreach (string path in _erroredPaths)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Failed Extract Paths
            if (_failedExtractPaths.Count > 0)
            {
                sw.WriteLine("Failed Extract Paths:");
                foreach (string path in _failedExtractPaths)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Flag Counts
            sw.WriteLine("Flag Counts");

            for (int i = 0; i < 32; i++)
            {
                sw.WriteLine($"  Bit {i}: {_flagCounts[i]}");
            }

            sw.WriteLine();

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
                _failedExtractPaths.Add(file);
            }
        }

        #endregion
    }
}
