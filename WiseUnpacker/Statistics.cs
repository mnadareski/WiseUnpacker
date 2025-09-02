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
        public Dictionary<string, PerFileStatistics> FilesMap { get; } = [];

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
        /// Process statistics for a WiseOverlayHeader
        /// </summary>
        /// <param name="file">Path of the file that contained the header</param>
        /// <param name="header">WiseOverlayHeader to gather statistics from</param>
        public void ProcessStatistics(string file, WiseOverlayHeader header)
        {
            if (!FilesMap.ContainsKey(file))
                FilesMap[file] = new();

            FilesMap[file].ProcessStatistics(header);
        }

        /// <summary>
        /// Process statistics for a WiseScript
        /// </summary>
        /// <param name="file">Path of the file that contained the script</param>
        /// <param name="script">WiseScript to gather statistics from</param>
        public void ProcessStatistics(string file, WiseScript script)
        {
            if (!FilesMap.ContainsKey(file))
                FilesMap[file] = new();

            FilesMap[file].ProcessStatistics(script);
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
                sw.WriteLine($"Invalid Paths: {_invalidPaths.Count}");
                _invalidPaths.Sort();
                foreach (string path in _invalidPaths)
                {
                    sw.WriteLine($"  {path}");
                }
            }

            // Errored Paths
            if (_erroredPaths.Count > 0)
            {
                sw.WriteLine($"Errored Paths: {_erroredPaths.Count}");
                _erroredPaths.Sort();
                foreach (string path in _erroredPaths)
                {
                    sw.WriteLine($"  {path}");
                }
            }

            // Failed Extract Paths
            if (_failedExtractPaths.Count > 0)
            {
                sw.WriteLine($"Failed Extract Paths: {_failedExtractPaths.Count}");
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
            Array.ForEach([.. FilesMap.Values], s =>
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
            Array.ForEach([.. FilesMap], kvp =>
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
                string filename = Test.PerFileStatistics.MapFileIndexToName(i);
                sw.WriteLine($"  {filename} ({i}): {shouldContainFile[i].Count}");
                shouldContainFile[i].Sort();
                foreach (string path in shouldContainFile[i])
                {
                    sw.WriteLine($"    {path}");
                }
            }

            sw.WriteLine();

            #endregion

            #region Header-Defined Files Hashes

            sw.WriteLine("Header-Defined Files Hashes:");

            var headerDefinedFilesHashes = new Dictionary<string, List<string>>[13];
            Array.ForEach([.. FilesMap], kvp =>
            {
                for (int i = 0; i < 13; i++)
                {
                    string? hash = kvp.Value.HeaderDefinedFileHashes[i];
                    if (hash == null)
                        continue;

                    if (headerDefinedFilesHashes[i] == default)
                        headerDefinedFilesHashes[i] = [];
                    if (!headerDefinedFilesHashes[i].ContainsKey(hash))
                        headerDefinedFilesHashes[i][hash] = [];

                    headerDefinedFilesHashes[i][hash].Add(kvp.Key);
                }
            });
            for (int i = 0; i < 13; i++)
            {
                if (headerDefinedFilesHashes[i] == null || headerDefinedFilesHashes[i].Count == 0)
                    continue;

                List<string> headerDefinedFileHashesKeys = [.. headerDefinedFilesHashes[i].Keys];
                headerDefinedFileHashesKeys.Sort();

                string filename = Test.PerFileStatistics.MapFileIndexToName(i);
                sw.WriteLine($"  {filename} ({i}):");

                for (int j = 0; j < headerDefinedFileHashesKeys.Count; j++)
                {
                    string hash = headerDefinedFileHashesKeys[j];
                    headerDefinedFilesHashes[i][hash].Sort();

                    sw.WriteLine($"    {hash}:");
                    foreach (string path in headerDefinedFilesHashes[i][hash])
                    {
                        sw.WriteLine($"      {path}");
                    }
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

            sw.WriteLine("First Flag:");

            Dictionary<ushort, List<string>> firstFlags = [];
            Array.ForEach([.. FilesMap], kvp =>
            {
                ushort? flag = kvp.Value.FirstFlag;
                if (flag == null)
                    return;

                if (!firstFlags.ContainsKey(flag.Value))
                    firstFlags[flag.Value] = [];

                firstFlags[flag.Value].Add(kvp.Key);
            });

            List<ushort> firstFlagsKeys = [.. firstFlags.Keys];
            firstFlagsKeys.Sort();

            foreach (ushort firstFlag in firstFlagsKeys)
            {
                sw.WriteLine($"  0x{firstFlag:X4}: {firstFlags[firstFlag].Count}");
                firstFlags[firstFlag].Sort();
                foreach (string path in firstFlags[firstFlag])
                {
                    sw.WriteLine($"    {path}");
                }
            }

            sw.WriteLine();

            #endregion

            #region Second Flags

            sw.WriteLine("Second Flag:");

            Dictionary<ushort, List<string>> secondFlags = [];
            Array.ForEach([.. FilesMap], kvp =>
            {
                ushort? flag = kvp.Value.SecondFlag;
                if (flag == null)
                    return;

                if (!secondFlags.ContainsKey(flag.Value))
                    secondFlags[flag.Value] = [];

                secondFlags[flag.Value].Add(kvp.Key);
            });

            List<ushort> secondFlagsKeys = [.. secondFlags.Keys];
            secondFlagsKeys.Sort();

            foreach (ushort secondFlag in secondFlagsKeys)
            {
                sw.WriteLine($"  0x{secondFlag:X4}: {secondFlags[secondFlag].Count}");
                secondFlags[secondFlag].Sort();
                foreach (string path in secondFlags[secondFlag])
                {
                    sw.WriteLine($"    {path}");
                }
            }

            sw.WriteLine();

            #endregion

            #region Third Flags

            sw.WriteLine("Third Flag:");

            Dictionary<ushort, List<string>> thirdFlags = [];
            Array.ForEach([.. FilesMap], kvp =>
            {
                ushort? flag = kvp.Value.ThirdFlag;
                if (flag == null)
                    return;

                if (!thirdFlags.ContainsKey(flag.Value))
                    thirdFlags[flag.Value] = [];

                thirdFlags[flag.Value].Add(kvp.Key);
            });

            List<ushort> thirdFlagsKeys = [.. thirdFlags.Keys];
            thirdFlagsKeys.Sort();

            foreach (ushort thirdFlag in thirdFlagsKeys)
            {
                sw.WriteLine($"  0x{thirdFlag:X4}: {thirdFlags[thirdFlag].Count}");
                thirdFlags[thirdFlag].Sort();
                foreach (string path in thirdFlags[thirdFlag])
                {
                    sw.WriteLine($"    {path}");
                }
            }

            sw.WriteLine();

            #endregion

            #region Header Prefix Lengths

            sw.WriteLine("Header Prefix Lengths:");

            Dictionary<int, List<string>> headerLengths = [];
            Array.ForEach([.. FilesMap], kvp =>
            {
                int? length = kvp.Value.HeaderPrefixLength;
                if (length == null)
                    return;

                if (!headerLengths.ContainsKey(length.Value))
                    headerLengths[length.Value] = [];

                headerLengths[length.Value].Add(kvp.Key);
            });

            List<int> headerLengthsKeys = [.. headerLengths.Keys];
            headerLengthsKeys.Sort();

            foreach (int length in headerLengthsKeys)
            {
                string lengthName = Test.PerFileStatistics.MapHeaderLengthToDescriptor(length);
                sw.WriteLine($"  {lengthName} ({length}): {headerLengths[length].Count}");
                headerLengths[length].Sort();
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
            Array.ForEach([.. FilesMap], kvp =>
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
            Array.ForEach([.. FilesMap], kvp =>
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
    }
}