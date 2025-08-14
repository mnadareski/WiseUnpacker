using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.Models.WiseInstaller;
using SabreTools.Models.WiseInstaller.Actions;
using SabreTools.Serialization;
using SabreTools.Serialization.Wrappers;

namespace Test
{
    /// <summary>
    /// Represents the statistics for a single installer
    /// </summary>
    internal class PerFileStatistics
    {
        #region Internal State

        #region Overlay Header

        /// <summary>
        /// Found header flags
        /// </summary>
        public bool[] Flags { get; } = new bool[32];

        /// <summary>
        /// Files that should be contained
        /// </summary>
        public bool[] ShouldContainFile { get; } = new bool[13];

        /// <summary>
        /// Inflated hashe of WISE0001.DLL
        /// </summary>
        public string? WiseDllHash { get; set; }

        #endregion

        #region Script

        #region Flags

        /// <summary>
        /// First flag in script file
        /// </summary>
        public ushort FirstFlag { get; private set; }

        /// <summary>
        /// Second flag in script file
        /// </summary>
        public ushort SecondFlag { get; private set; }

        /// <summary>
        /// Third flag in script file
        /// </summary>
        public ushort ThirdFlag { get; private set; }

        #endregion

        /// <summary>
        /// Date field from the header
        /// </summary>
        public uint Datetime { get; private set; }

        /// <summary>
        /// List of found DLL function calls
        /// </summary>
        public List<string> Functions { get; } = [];

        /// <summary>
        /// Length of the header prefix
        /// </summary>
        /// <remarks>
        /// Common lengths:
        /// - 18 bytes (Short)
        /// - 38 bytes (Middle)
        /// - 43 bytes (Normal)
        /// </remarks>
        public int HeaderPrefixLength { get; private set; } = -1;

        /// <summary>
        /// List of found opcodes
        /// </summary>
        public List<OperationCode> Opcodes { get; } = [];

        #endregion

        #endregion

        #region Processing

        /// <summary>
        /// Process statistics for a WiseOverlayHeader
        /// </summary>
        /// <param name="header">WiseOverlayHeader to gather statistics from</param>
        public void ProcessStatistics(WiseOverlayHeader header)
        {
            // Flags
            for (int i = 0; i < 32; i++)
            {
                int flags = (int)header.Flags;
                int compare = 1 << i;

                if ((flags & compare) == compare)
                    Flags[i] = true;
            }

            // Contained Files
            if (header.DibDeflatedSize > 0)
                ShouldContainFile[0] = true;
            if (header.WiseScriptDeflatedSize > 0)
                ShouldContainFile[1] = true;
            if (header.WiseDllDeflatedSize > 0)
                ShouldContainFile[2] = true;
            if (header.Ctl3d32DeflatedSize > 0)
                ShouldContainFile[3] = true;
            if (header.SomeData4DeflatedSize > 0)
                ShouldContainFile[4] = true;
            if (header.RegToolDeflatedSize > 0)
                ShouldContainFile[5] = true;
            if (header.ProgressDllDeflatedSize > 0)
                ShouldContainFile[6] = true;
            if (header.SomeData7DeflatedSize > 0)
                ShouldContainFile[7] = true;
            if (header.SomeData8DeflatedSize > 0)
                ShouldContainFile[8] = true;
            if (header.SomeData9DeflatedSize > 0)
                ShouldContainFile[9] = true;
            if (header.SomeData10DeflatedSize > 0)
                ShouldContainFile[10] = true;
            if (header.InstallScriptDeflatedSize > 0)
                ShouldContainFile[11] = true;
            if (header.FinalFileDeflatedSize > 0)
                ShouldContainFile[12] = true;
        }

        /// <summary>
        /// Process statistics for a WiseScript
        /// </summary>
        /// <param name="script">WiseScript to gather statistics from</param>
        public void ProcessStatistics(WiseScript script)
        {
            // First Flags
            FirstFlag = script.Model.Header?.Flags ?? 0;
            SecondFlag = script.Model.Header?.UnknownU16_1 ?? 0;
            ThirdFlag = script.Model.Header?.UnknownU16_2 ?? 0;

            // Datetime
            Datetime = script.Model.Header?.DateTime ?? 0;

            // Header Length
            if (script.Model.Header?.Unknown_22 != null && script.Model.Header.Unknown_22.Length != 22)
            {
                if (script.Model.Header.DateTime == 0x00000000)
                    HeaderPrefixLength = 18;
                else
                    HeaderPrefixLength = 38;
            }
            else
            {
                HeaderPrefixLength = 43;
            }

            // Actions
            foreach (var state in script.States ?? [])
            {
                if (!Opcodes.Contains(state.Op))
                    Opcodes.Add(state.Op);
            }

            // Function Calls
            if (script.States != null && Array.Exists(script.States, s => s.Op == OperationCode.CallDllFunction))
            {
                var states = Array.FindAll(script.States, s => s.Op == OperationCode.CallDllFunction);
                foreach (var state in states)
                {
                    if (state.Data is not CallDllFunction function)
                        continue;

                    string functionName = function.FunctionName ?? "INVALID";
                    if (!Functions.Contains(functionName))
                        Functions.Add(functionName);
                }
            }
        }

        #endregion

        #region Printing

        /// <summary>
        /// Export overlay header statistics
        /// </summary>
        /// <param name="sw">StreamWriter representing the output</param>
        public void ExportOverlayHeaderStatistics(StreamWriter sw)
        {
            sw.WriteLine("Overlay Header");
            sw.WriteLine("-------------------------");

            // Flags
            sw.WriteLine("Flags:");
            for (int i = 0; i < Flags.Length; i++)
            {
                uint bitValue = 1u << i;
                string bitName = Enum.GetName(typeof(OverlayHeaderFlags), bitValue) ?? "Undefined";
                sw.WriteLine($"  Bit {i} ({bitName}): {Flags[i]}");
            }

            sw.WriteLine();

            // Should Contain File
            sw.WriteLine("Should Contain File:");
            for (int i = 0; i < ShouldContainFile.Length; i++)
            {
                string filename = MapFileIndexToName(i);
                sw.WriteLine($"  {filename} ({i}): {ShouldContainFile[i]}");
            }

            sw.WriteLine();

            // WISE0001.DLL Hashe
            sw.WriteLine($"WISE0001.DLL Hash: {WiseDllHash}");
            sw.WriteLine();

            sw.Flush();
        }

        /// <summary>
        /// Export script statistics
        /// </summary>
        /// <param name="sw">StreamWriter representing the output</param>
        public void ExportScriptStatistics(StreamWriter sw)
        {
            sw.WriteLine("Script");
            sw.WriteLine("-------------------------");

            // First Flags
            sw.WriteLine($"First Flag: {FirstFlag}");
            sw.WriteLine($"Second Flag: {SecondFlag}");
            sw.WriteLine($"Third Flag: {ThirdFlag}");
            sw.WriteLine();

            // Datetime
            sw.WriteLine($"Datetime: {Datetime}"); // TODO: Translate to human-readable
            sw.WriteLine();

            // Header Length
            sw.WriteLine($"Header Prefix Length: {HeaderPrefixLength} ({MapHeaderLengthToDescriptor(HeaderPrefixLength)})");
            sw.WriteLine();

            // Contains Invalid Actions
            sw.WriteLine("Contains Invalid Actions:");
            sw.WriteLine($"  Invalid0x01: {Opcodes.Contains(OperationCode.Invalid0x01)}");
            sw.WriteLine($"  Invalid0x0E: {Opcodes.Contains(OperationCode.Invalid0x0E)}");
            sw.WriteLine($"  Invalid0x13: {Opcodes.Contains(OperationCode.Invalid0x13)}");
            sw.WriteLine($"  Invalid0x1F: {Opcodes.Contains(OperationCode.Invalid0x1F)}");
            sw.WriteLine($"  Invalid0x20: {Opcodes.Contains(OperationCode.Invalid0x20)}");
            sw.WriteLine($"  Invalid0x21: {Opcodes.Contains(OperationCode.Invalid0x21)}");
            sw.WriteLine($"  Invalid0x22: {Opcodes.Contains(OperationCode.Invalid0x22)}");
            sw.WriteLine();

            // Contains Unmapped Actions
            sw.WriteLine("Contains Unmapped Actions:");
            sw.WriteLine($"  InstallODBCDriver: {Opcodes.Contains(OperationCode.InstallODBCDriver)}");
            sw.WriteLine($"  Unknown0x24: {Opcodes.Contains(OperationCode.Unknown0x24)}");
            sw.WriteLine($"  Unknown0x25: {Opcodes.Contains(OperationCode.Unknown0x25)}");
            sw.WriteLine();

            // Contains Unmapped Function
            var unmappedFunctions = Functions.FindAll(k =>
            {
                string? functionName = k.FromWiseFunctionId();
                return functionName == null || functionName.StartsWith("UNDEFINED");
            });
            sw.WriteLine($"Contains Unmapped Function: {unmappedFunctions.Count > 0}");
            sw.WriteLine();

            sw.Flush();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Map a file index to the output name
        /// </summary>
        /// <param name="index">File index to map</param>
        /// <returns>Mapped name, if possible</returns>
        public static string MapFileIndexToName(int index)
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
        public static string MapHeaderLengthToDescriptor(int length)
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