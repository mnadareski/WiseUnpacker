using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.Models.WiseInstaller;
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

        #region Per-File Statistics

        /// <summary>
        /// Per-file statistics map
        /// </summary>
        private readonly Dictionary<string, PerFileStatistics> _perFileStatistics = [];

        #endregion

        #endregion

        #region Processing

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
        /// Add a WISE0001.DLL size
        /// </summary>
        /// <param name="file">Path of the file containing the compressed DLL</param>
        /// <param name="hash">Hash of the inflated DLL</param>
        public void AddWiseDllHash(string file, string hash)
        {
            if (!_perFileStatistics.ContainsKey(file))
                _perFileStatistics[file] = new();

            _perFileStatistics[file].WiseDllHash = hash;
        }

        /// <summary>
        /// Process statistics for a WiseOverlayHeader
        /// </summary>
        /// <param name="file">Path of the file that contained the header</param>
        /// <param name="header">WiseOverlayHeader to gather statistics from</param>
        public void ProcessStatistics(string file, WiseOverlayHeader header)
        {
            if (!_perFileStatistics.ContainsKey(file))
                _perFileStatistics[file] = new();

            _perFileStatistics[file].ProcessStatistics(header);
        }

        /// <summary>
        /// Process statistics for a WiseScript
        /// </summary>
        /// <param name="file">Path of the file that contained the script</param>
        /// <param name="script">WiseScript to gather statistics from</param>
        public void ProcessStatistics(string file, WiseScript script)
        {
            if (!_perFileStatistics.ContainsKey(file))
                _perFileStatistics[file] = new();

            _perFileStatistics[file].ProcessStatistics(script);
        }

        #endregion

        #region Printing

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

            #region Flag Counts

            sw.WriteLine("Flag Counts:");

            int[] flagCounts = new int[32];
            Array.ForEach([.. _perFileStatistics.Values], s =>
            {
                for (int i = 0; i < flagCounts.Length; i++)
                {
                    flagCounts[i] += s.Flags[i] ? 1 : 0;
                }
            });
            for (int i = 0; i < flagCounts.Length; i++)
            {
                uint bitValue = 1u << i;
                string bitName = Enum.GetName(typeof(OverlayHeaderFlags), bitValue) ?? "Undefined";
                sw.WriteLine($"  Bit {i} ({bitName}): {flagCounts[i]}");
            }

            sw.WriteLine();

            #endregion

            #region Should Contain File

            sw.WriteLine("Should Contain File:");

            var shouldContainFile = new List<string>[13];
            Array.ForEach([.. _perFileStatistics], kvp =>
            {
                for (int i = 0; i < shouldContainFile.Length; i++)
                {
                    if (shouldContainFile[i] == default)
                        shouldContainFile[i] = [];

                    if (kvp.Value.ShouldContainFile[i])
                        shouldContainFile[i].Add(kvp.Key);
                }
            });
            for (int i = 0; i < shouldContainFile.Length; i++)
            {
                string filename = MapFileIndexToName(i);
                sw.WriteLine($"  {filename} ({i}): {shouldContainFile[i].Count}");
                foreach (string path in shouldContainFile[i])
                {
                    sw.WriteLine($"    {path}");
                }
            }

            sw.WriteLine();

            #endregion

            #region WISE0001.DLL Hashes

            sw.WriteLine("WISE0001.DLL Hashes:");

            Dictionary<string, List<string>> wiseDllHashes = [];
            Array.ForEach([.. _perFileStatistics], kvp =>
            {
                string? hash = kvp.Value.WiseDllHash;
                if (hash != null && !wiseDllHashes.ContainsKey(hash))
                    wiseDllHashes[hash] = [];

                if (hash != null)
                    wiseDllHashes[hash].Add(kvp.Key);
            });

            List<string> wiseDllHashesKeys = [.. wiseDllHashes.Keys];
            wiseDllHashesKeys.Sort();

            foreach (string hash in wiseDllHashesKeys)
            {
                sw.WriteLine($"  {hash}: {wiseDllHashes[hash].Count}");
                foreach (string path in wiseDllHashes[hash])
                {
                    sw.WriteLine($"    {path}");
                }
            }

            sw.WriteLine();

            #endregion

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

            #region First Flags

            sw.WriteLine("First Flags:");

            Dictionary<ushort, List<string>> firstFlags = [];
            Array.ForEach([.. _perFileStatistics], kvp =>
            {
                ushort flag = kvp.Value.FirstFlag;
                if (!firstFlags.ContainsKey(flag))
                    firstFlags[flag] = [];

                firstFlags[flag].Add(kvp.Key);
            });

            List<ushort> firstFlagsKeys = [.. firstFlags.Keys];
            firstFlagsKeys.Sort();

            foreach (ushort firstFlag in firstFlagsKeys)
            {
                sw.WriteLine($"  0x{firstFlag:X4}: {firstFlags[firstFlag].Count}");
                foreach (string path in firstFlags[firstFlag])
                {
                    sw.WriteLine($"    {path}");
                }
            }

            sw.WriteLine();

            #endregion

            #region Header Prefix Lengths

            sw.WriteLine("Header Prefix Lengths:");

            Dictionary<int, List<string>> headerLengths = [];
            Array.ForEach([.. _perFileStatistics], kvp =>
            {
                int length = kvp.Value.HeaderPrefixLength;
                if (!headerLengths.ContainsKey(length))
                    headerLengths[length] = [];

                headerLengths[length].Add(kvp.Key);
            });

            List<int> headerLengthsKeys = [.. headerLengths.Keys];
            headerLengthsKeys.Sort();

            foreach (int length in headerLengthsKeys)
            {
                string lengthName = MapHeaderLengthToDescriptor(length);
                sw.WriteLine($"  {lengthName} ({length}): {headerLengths[length].Count}");
                foreach (string path in headerLengths[length])
                {
                    sw.WriteLine($"    {path}");
                }
            }

            sw.WriteLine();

            #endregion

            #region Opcodes

            var enumValues = (OperationCode[])Enum.GetValues(typeof(OperationCode));
            Dictionary<OperationCode, List<string>> opcodes = [];
            Array.ForEach([.. _perFileStatistics], kvp =>
            {
                foreach (var enumValue in enumValues)
                {
                    bool containsValue = kvp.Value.Opcodes.Contains(enumValue);
                    if (containsValue && !opcodes.ContainsKey(enumValue))
                        opcodes[enumValue] = [];

                    if (containsValue)
                        opcodes[enumValue].Add(kvp.Key);
                }
            });

            // Contains Invalid0x01
            if (opcodes.TryGetValue(OperationCode.Invalid0x01, out var contains0x01) && contains0x01.Count > 0)
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
            if (opcodes.TryGetValue(OperationCode.Invalid0x0E, out var contains0x0E) && contains0x0E.Count > 0)
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
            if (opcodes.TryGetValue(OperationCode.Invalid0x13, out var contains0x13) && contains0x13.Count > 0)
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
            if (opcodes.TryGetValue(OperationCode.InstallODBCDriver, out var contains0x19) && contains0x19.Count > 0)
            {
                sw.WriteLine("Contains InstallODBCDriver:");
                contains0x19.Sort();
                foreach (string path in contains0x19)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Invalid0x1F
            if (opcodes.TryGetValue(OperationCode.Invalid0x1F, out var contains0x1F) && contains0x1F.Count > 0)
            {
                sw.WriteLine("Contains Invalid0x1F:");
                contains0x1F.Sort();
                foreach (string path in contains0x1F)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Invalid0x20
            if (opcodes.TryGetValue(OperationCode.Invalid0x20, out var contains0x20) && contains0x20.Count > 0)
            {
                sw.WriteLine("Contains Invalid0x20:");
                contains0x20.Sort();
                foreach (string path in contains0x20)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Invalid0x21
            if (opcodes.TryGetValue(OperationCode.Invalid0x21, out var contains0x21) && contains0x21.Count > 0)
            {
                sw.WriteLine("Contains Invalid0x21:");
                contains0x21.Sort();
                foreach (string path in contains0x21)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Invalid0x22
            if (opcodes.TryGetValue(OperationCode.Invalid0x22, out var contains0x22) && contains0x22.Count > 0)
            {
                sw.WriteLine("Contains Invalid0x22:");
                contains0x22.Sort();
                foreach (string path in contains0x22)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            // Contains Unknown0x24
            if (opcodes.TryGetValue(OperationCode.Unknown0x24, out var contains0x24) && contains0x24.Count > 0)
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
            if (opcodes.TryGetValue(OperationCode.Unknown0x25, out var contains0x25) && contains0x25.Count > 0)
            {
                sw.WriteLine("Contains Unknown0x25:");
                contains0x25.Sort();
                foreach (string path in contains0x25)
                {
                    sw.WriteLine($"  {path}");
                }

                sw.WriteLine();
            }

            #endregion

            #region Functions

            Dictionary<string, List<string>> functions = [];
            Array.ForEach([.. _perFileStatistics], kvp =>
            {
                foreach (string function in kvp.Value.Functions)
                {
                    if (!functions.ContainsKey(function))
                        functions[function] = [];

                    functions[function].Add(kvp.Key);
                }
            });

            // Contains Unmapped Function
            var unmappedFunctions = Array.FindAll([.. functions.Keys], k =>
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
                    foreach (string path in functions[function])
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

            #endregion

            sw.Flush();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Map a file index to the output name
        /// </summary>
        /// <param name="index">File index to map</param>
        /// <returns>Mapped name, if possible</returns>
        private static string MapFileIndexToName(int index)
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

        /// <summary>
        /// Map a header length to a known descriptor
        /// </summary>
        /// <param name="length">Length to map</param>
        /// <returns>Mapped descriptor, if possible</returns>
        private static string MapHeaderLengthToDescriptor(int length)
        {
            return length switch
            {
                18 => "Short",
                38 => "Middle",
                43 => "Normal",
                _ => $"Unknown Length {length}",
            };
        }

        #endregion
    }
}