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
        /// Mapping of first flags in script files
        /// </summary>
        private readonly Dictionary<ushort, List<string>> _firstFlags = [];

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
            // First Flags
            ushort flags = script.Model.Header?.Flags ?? 0;
            if (!_firstFlags.ContainsKey(script.Model.Header?.Flags ?? 0))
                _firstFlags[flags] = [];

            _firstFlags[flags].Add(file);

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
                _invalidPaths.Sort();
                foreach (string path in _invalidPaths)
                {
                    sw.WriteLine($"  {path}");
                }
            }

            // Errored Paths
            if (_erroredPaths.Count > 0)
            {
                sw.WriteLine("Errored Paths:");
                _erroredPaths.Sort();
                foreach (string path in _erroredPaths)
                {
                    sw.WriteLine($"  {path}");
                }
            }

            // Failed Extract Paths
            if (_failedExtractPaths.Count > 0)
            {
                sw.WriteLine("Failed Extract Paths:");
                _failedExtractPaths.Sort();
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
                uint bitValue = 1u << i;
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

            // First Flags
            if (_firstFlags.Count > 0)
            {
                sw.WriteLine("First Flags:");
                List<ushort> firstFlagsKeys = [.. _firstFlags.Keys];
                firstFlagsKeys.Sort();

                foreach (byte flags in firstFlagsKeys)
                {
                    sw.WriteLine($"  0x{flags:X4}:");
                    foreach (string path in _firstFlags[flags])
                    {
                        sw.WriteLine($"    {path}");
                    }
                }

                sw.WriteLine();
            }

            // Short Headers
            if (_shortHeaders.Count > 0)
            {
                sw.WriteLine("Short Header:");
                _shortHeaders.Sort();
                foreach (string path in _shortHeaders)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Invalid0x01
            if (_opcodes.TryGetValue(OperationCode.Invalid0x01, out var contains0x01) && contains0x01.Count > 0)
            {
                sw.WriteLine("Contains Invalid0x01:");
                contains0x01.Sort();
                foreach (string path in contains0x01)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Invalid0x0E
            if (_opcodes.TryGetValue(OperationCode.Invalid0x0E, out var contains0x0E) && contains0x0E.Count > 0)
            {
                sw.WriteLine("Contains Invalid0x0E:");
                contains0x0E.Sort();
                foreach (string path in contains0x0E)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Invalid0x13
            if (_opcodes.TryGetValue(OperationCode.Invalid0x13, out var contains0x13) && contains0x13.Count > 0)
            {
                sw.WriteLine("Contains Invalid0x13:");
                contains0x13.Sort();
                foreach (string path in contains0x13)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains InstallODBCDriver
            if (_opcodes.TryGetValue(OperationCode.InstallODBCDriver, out var contains0x19) && contains0x19.Count > 0)
            {
                sw.WriteLine("Contains InstallODBCDriver:");
                contains0x19.Sort();
                foreach (string path in contains0x19)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unknown0x1F
            if (_opcodes.TryGetValue(OperationCode.Unknown0x1F, out var contains0x1F) && contains0x1F.Count > 0)
            {
                sw.WriteLine("Contains Unknown0x1F:");
                contains0x1F.Sort();
                foreach (string path in contains0x1F)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unknown0x20
            if (_opcodes.TryGetValue(OperationCode.Unknown0x20, out var contains0x20) && contains0x20.Count > 0)
            {
                sw.WriteLine("Contains Unknown0x20:");
                contains0x20.Sort();
                foreach (string path in contains0x20)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unknown0x21
            if (_opcodes.TryGetValue(OperationCode.Unknown0x21, out var contains0x21) && contains0x21.Count > 0)
            {
                sw.WriteLine("Contains Unknown0x21:");
                contains0x21.Sort();
                foreach (string path in contains0x21)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unknown0x22
            if (_opcodes.TryGetValue(OperationCode.Unknown0x22, out var contains0x22) && contains0x22.Count > 0)
            {
                sw.WriteLine("Contains Unknown0x22:");
                contains0x22.Sort();
                foreach (string path in contains0x22)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unknown0x24
            if (_opcodes.TryGetValue(OperationCode.Unknown0x24, out var contains0x24) && contains0x24.Count > 0)
            {
                sw.WriteLine("Contains Unknown0x24:");
                contains0x24.Sort();
                foreach (string path in contains0x24)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unknown0x25
            if (_opcodes.TryGetValue(OperationCode.Unknown0x25, out var contains0x25) && contains0x25.Count > 0)
            {
                sw.WriteLine("Contains Unknown0x25:");
                contains0x25.Sort();
                foreach (string path in contains0x25)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains f0 -- Known but need samples for data layout
            if (_functions.TryGetValue("f0", out var containsFunction0) && containsFunction0.Count > 0)
            {
                sw.WriteLine("Contains Function f0 [Add Directory to PATH]:");
                containsFunction0.Sort();
                foreach (string path in containsFunction0)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unmapped Function
            var unmappedFunctions = Array.FindAll([.. _functions.Keys], k =>
            {
                string? functionName = k.FromWiseFunctionId();
                return functionName == null || functionName.StartsWith("UNDEFINED");
            });
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
                    containsUnmappedFunction.Sort();
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