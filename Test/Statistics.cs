using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.Models.WiseInstaller;
using SabreTools.Models.WiseInstaller.Actions;
using SabreTools.Serialization;
using SabreTools.Serialization.Wrappers;

namespace Test
{
    internal class Statistics
    {
        #region Internal State

        #region File Errors

        /// <summary>
        /// All paths that threw an exception during parsing
        /// </summary>
        private readonly List<string> _erroredPaths = [];

        /// <summary>
        /// All paths that failed to extract all items
        /// </summary>
        private readonly List<string> _failedExtractPaths = [];

        /// <summary>
        /// All paths that were marked as invalid
        /// </summary>
        private readonly List<string> _invalidPaths = [];

        #endregion

        #region Overlay Header

        /// <summary>
        /// Mapping of found header flags
        /// </summary>
        private readonly List<string>[] _flags = new List<string>[32];

        /// <summary>
        /// Mapping for files that should be contained
        /// </summary>
        private readonly List<string>[] _shouldContainFile = new List<string>[13];

        #endregion

        #region Script

        /// <summary>
        /// Mapping of found DLL function calls
        /// </summary>
        private readonly Dictionary<string, List<string>> _functions = [];

        /// <summary>
        /// Mapping of found opcodes
        /// </summary>
        private readonly Dictionary<OperationCode, List<string>> _opcodes = [];

        /// <summary>
        /// All paths that have "short" headers
        /// </summary>
        private readonly List<string> _shortHeaders = [];

        #endregion

        #endregion

        public Statistics()
        {
            for (int i = 0; i < _flags.Length; i++)
            {
                _flags[i] = [];
            }

            for (int i = 0; i < _shouldContainFile.Length; i++)
            {
                _shouldContainFile[i] = [];
            }
        }

        /// <summary>
        /// Add an errored file path
        /// </summary>
        /// <param name="file">Path of the file that was errored</param>
        public void AddErroredPath(string file)
        {
            if (!_erroredPaths.Contains(file))
                _erroredPaths.Add(file);
        }

        /// <summary>
        /// Add a failed extract file path
        /// </summary>
        /// <param name="file">Path of the file that was failed extraction</param>
        public void AddFailedExtractPath(string file)
        {
            if (!_failedExtractPaths.Contains(file))
                _failedExtractPaths.Add(file);
        }

        /// <summary>
        /// Add an invalid file path
        /// </summary>
        /// <param name="file">Path of the file that was invalid</param>
        public void AddInvalidPath(string file)
        {
            if (!_invalidPaths.Contains(file))
                _invalidPaths.Add(file);
        }

        /// <summary>
        /// Process statistics for a WiseOverlayHeader
        /// </summary>
        /// <param name="file">Path of the file that contained the header</param>
        /// <param name="header">WiseOverlayHeader to gather statistics from</param>
        public void ProcessStatistics(string file, WiseOverlayHeader header)
        {
            // Flags
            for (int i = 0; i < 32; i++)
            {
                int flags = (int)header.Flags;
                int compare = 1 << i;

                if ((flags & compare) == compare)
                {
                    // Ensure the key
                    if (_flags[i] == null)
                        _flags[i] = [];

                    // Store each file only once
                    if (!_flags[i].Contains(file))
                        _flags[i].Add(file);
                }
            }

            // Contained Files
            if (header.DibDeflatedSize > 0)
                _shouldContainFile[0].Add(file);
            if (header.WiseScriptDeflatedSize > 0)
                _shouldContainFile[1].Add(file);
            if (header.WiseDllDeflatedSize > 0)
                _shouldContainFile[2].Add(file);
            if (header.Ctl3d32DeflatedSize > 0)
                _shouldContainFile[3].Add(file);
            if (header.SomeData4DeflatedSize > 0)
                _shouldContainFile[4].Add(file);
            if (header.RegToolDeflatedSize > 0)
                _shouldContainFile[5].Add(file);
            if (header.ProgressDllDeflatedSize > 0)
                _shouldContainFile[6].Add(file);
            if (header.SomeData7DeflatedSize > 0)
                _shouldContainFile[7].Add(file);
            if (header.SomeData8DeflatedSize > 0)
                _shouldContainFile[8].Add(file);
            if (header.SomeData9DeflatedSize > 0)
                _shouldContainFile[9].Add(file);
            if (header.SomeData10DeflatedSize > 0)
                _shouldContainFile[10].Add(file);
            if (header.InstallScriptDeflatedSize > 0)
                _shouldContainFile[11].Add(file);
            if (header.FinalFileDeflatedSize > 0)
                _shouldContainFile[12].Add(file);
        }

        /// <summary>
        /// Process statistics for a WiseScript
        /// </summary>
        /// <param name="file">Path of the file that contained the script</param>
        /// <param name="script">WiseScript to gather statistics from</param>
        public void ProcessStatistics(string file, WiseScript script)
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
        public void ExportStatistics()
        {
            using var sw = new StreamWriter(File.OpenWrite($"stats-{DateTime.Now:yyyy-MM-dd_HHmmss.ffff}.txt"));

            ExportErrorStatistics(sw);
            ExportOverlayHeaderStatistics(sw);
            ExportScriptStatistics(sw);
        }

        /// <summary>
        /// Export file error statistics
        /// </summary>
        /// <param name="sw">StreamWriter representing the output</param>
        private void ExportErrorStatistics(StreamWriter sw)
        {
            sw.WriteLine("File Errors");
            sw.WriteLine("-------------------------");

            // Invalid Paths
            if (_invalidPaths.Count > 0)
            {
                sw.WriteLine("Invalid Paths:");
                foreach (string path in _invalidPaths)
                {
                    sw.WriteLine($"  {path}");
                }
            }

            // Errored Paths
            if (_erroredPaths.Count > 0)
            {
                sw.WriteLine("Errored Paths:");
                foreach (string path in _erroredPaths)
                {
                    sw.WriteLine($"  {path}");
                }
            }

            // Failed Extract Paths
            if (_failedExtractPaths.Count > 0)
            {
                sw.WriteLine("Failed Extract Paths:");
                foreach (string path in _failedExtractPaths)
                {
                    sw.WriteLine($"  {path}");
                }
            }

            sw.WriteLine();
            sw.Flush();
        }

        /// <summary>
        /// Export overlay header statistics
        /// </summary>
        /// <param name="sw">StreamWriter representing the output</param>
        private void ExportOverlayHeaderStatistics(StreamWriter sw)
        {
            sw.WriteLine("Overlay Header");
            sw.WriteLine("-------------------------");

            // Flag Counts
            sw.WriteLine("Flag Counts:");
            for (int i = 0; i < _flags.Length; i++)
            {
                int bitValue = 1 << i;
                string bitName = Enum.GetName(typeof(OverlayHeaderFlags), bitValue) ?? "Undefined";
                sw.WriteLine($"  Bit {i} ({bitName}): {_flags[i].Count}");
            }

            // Should Contain File
            sw.WriteLine("Should Contain File:");
            for (int i = 0; i < _shouldContainFile.Length; i++)
            {
                string filename = MapFileIndexToName(i);
                sw.WriteLine($"  {filename} ({i}): {_shouldContainFile[i].Count}");
            }

            sw.WriteLine();
            sw.Flush();
        }

        /// <summary>
        /// Export script statistics
        /// </summary>
        /// <param name="sw">StreamWriter representing the output</param>
        private void ExportScriptStatistics(StreamWriter sw)
        {
            sw.WriteLine("Script");
            sw.WriteLine("-------------------------");

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

            sw.WriteLine();
            sw.Flush();
        }

        /// <summary>
        /// Map a file index to the output name
        /// </summary>
        /// <param name="index">File index to map</param>
        /// <returns>Mapped name, if possible</returns>
        private string MapFileIndexToName(int index)
        {
            return index switch
            {
                0 => "WiseColors.dib",
                1 => "WiseScript.bin",
                2 => "WISE0001.DLL",
                3 => "CTL3D32.DLL",
                4 => "FILE0004",
                5 => "Ocxreg32.EXE",
                6 => "PROGRESS.DLL",
                7 => "FILE0007",
                8 => "FILE0008",
                9 => "FILE0009",
                10 => "FILE000A",
                11 => "INSTALL_SCRIPT",
                12 => "FILE0XX.DAT",
                _ => $"Unknown File {index}",
            };
        }
    }
}