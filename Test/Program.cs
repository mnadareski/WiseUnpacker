using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.Models.WiseInstaller.Actions;
using SabreTools.Serialization;
using SabreTools.Serialization.Wrappers;
using OperationCode = SabreTools.Models.WiseInstaller.OperationCode;

namespace Test
{
    class Program
    {
        #region Statistics

        /// <summary>
        /// Mapping of found DLL function calls
        /// </summary>
        private static readonly Dictionary<string, List<string>> _functions = [];

        /// <summary>
        /// Mapping of found opcodes
        /// </summary>
        private static readonly Dictionary<OperationCode, List<string>> _opcodes = [];

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

        /// <summary>
        /// All paths that have "short" headers
        /// </summary>
        private static readonly List<string> _shortHeaders = [];

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

                // Process script statistics
                ProcessStatistics(file, script);

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
        /// Process statistics for a WiseScript
        /// </summary>
        private static void ProcessStatistics(string file, WiseScript script)
        {
            // Short Header
            if (script.Model.Header?.Unknown_22 != null && script.Model.Header.Unknown_22.Length != 22)
                _shortHeaders.Add(file);

            // Actions
            if (script.States != null)
            {
                foreach (var state in script.States)
                {
                    // Ensure the key
                    if (!_opcodes.ContainsKey(state.Op))
                        _opcodes[state.Op] = [];

                    // Store each file only once
                    if (!_opcodes[state.Op].Contains(file))
                        _opcodes[state.Op].Add(file);
                }
            }

            // Function Calls
            if (script.States != null && Array.Exists(script.States, s => s.Op == OperationCode.CallDllFunction))
            {
                var states = Array.FindAll(script.States, s => s.Op == OperationCode.CallDllFunction);
                foreach (var state in states)
                {
                    // Get the function as an item
                    if (state.Data is not CallDllFunction function)
                        continue;

                    // Ensure the key
                    string functionName = function.FunctionName ?? "INVALID";
                    if (!_functions.ContainsKey(functionName))
                        _functions[functionName] = [];

                    // Store each file only once
                    if (!_functions[functionName].Contains(file))
                        _functions[functionName].Add(file);
                }
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

            // Short Headers
            if (_shortHeaders.Count > 0)
            {
                sw.WriteLine("Short Header:");
                foreach (string path in _shortHeaders)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unknown0x06
            if (_opcodes.TryGetValue(OperationCode.UnknownDeflatedFile0x06, out var contains0x06) && contains0x06.Count > 0)
            {
                sw.WriteLine("Contains Unknown0x06:");
                foreach (string path in contains0x06)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unknown0x19
            if (_opcodes.TryGetValue(OperationCode.Unknown0x19, out var contains0x19) && contains0x19.Count > 0)
            {
                sw.WriteLine("Contains Unknown0x19:");
                foreach (string path in contains0x19)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains f1
            if (_functions.TryGetValue("f1", out var containsFunction1) && containsFunction1.Count > 0)
            {
                sw.WriteLine("Contains Function f1:");
                foreach (string path in containsFunction1)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains f28
            if (_functions.TryGetValue("f28", out var containsFunction28) && containsFunction28.Count > 0)
            {
                sw.WriteLine("Contains Function f28:");
                foreach (string path in containsFunction28)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains f30
            if (_functions.TryGetValue("f30", out var containsFunction30) && containsFunction30.Count > 0)
            {
                sw.WriteLine("Contains Function f30:");
                foreach (string path in containsFunction30)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unmapped Function
            var unmappedFunctions = Array.FindAll([.. _functions.Keys], k => k.FromWiseFunctionId() == null);
            if (unmappedFunctions.Length > 0)
            {
                // Build unique file path list
                List<string> containsUnmappedFunction = [];
                foreach (string function in unmappedFunctions)
                {
                    foreach (string path in _functions[function])
                    {
                        if (!containsUnmappedFunction.Contains(path))
                            containsUnmappedFunction.Add(path);
                    }
                }

                if (containsUnmappedFunction.Count > 0)
                {
                    sw.WriteLine("Contains Unmapped Function:");
                    foreach (string path in containsUnmappedFunction)
                    {
                        sw.WriteLine($"  {path}");
                    }

                    sw.WriteLine();
                }
            }

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
