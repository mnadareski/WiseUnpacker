using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.Models.WiseInstaller.Actions;
using SabreTools.Serialization;
using SabreTools.Serialization.Wrappers;
using OperationCode = SabreTools.Models.WiseInstaller.OperationCode;

namespace Test
{
    internal class Statistics
    {
        #region Internal State

        /// <summary>
        /// Mapping of found DLL function calls
        /// </summary>
        private readonly Dictionary<string, List<string>> _functions = [];

        /// <summary>
        /// Mapping of found opcodes
        /// </summary>
        private readonly Dictionary<OperationCode, List<string>> _opcodes = [];

        /// <summary>
        /// All paths that threw an exception during parsing
        /// </summary>
        private readonly List<string> _erroredPaths = [];

        /// <summary>
        /// All paths that failed to extract all items
        /// </summary>
        private readonly List<string> _failedExtractPaths = [];

        /// <summary>
        /// Mapping of found header flags
        /// </summary>
        private readonly List<string>[] _flags = new List<string>[32];

        /// <summary>
        /// All paths that were marked as invalid
        /// </summary>
        private readonly List<string> _invalidPaths = [];

        /// <summary>
        /// All paths that have "short" headers
        /// </summary>
        private readonly List<string> _shortHeaders = [];

        #endregion

        /// <summary>
        /// Add an errored file path
        /// </summary>
        public void AddErroredPath(string file)
        {
            if (!_erroredPaths.Contains(file))
                _erroredPaths.Add(file);
        }

        /// <summary>
        /// Add a failed extract file path
        /// </summary>
        public void AddFailedExtractPath(string file)
        {
            if (!_failedExtractPaths.Contains(file))
                _failedExtractPaths.Add(file);
        }

        /// <summary>
        /// Add an invalid file path
        /// </summary>
        public void AddInvalidPath(string file)
        {
            if (!_invalidPaths.Contains(file))
                _invalidPaths.Add(file);
        }

        /// <summary>
        /// Process statistics for a WiseOverlayHeader
        /// </summary>
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
        }

        /// <summary>
        /// Process statistics for a WiseScript
        /// </summary>
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
                sw.WriteLine($"  Bit {i}: {_flags[i].Count}");
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
    }
}