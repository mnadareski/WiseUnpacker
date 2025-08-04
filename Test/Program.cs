using System;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.Serialization;
using SabreTools.Serialization.Wrappers;

namespace Test
{
    class Program
    {
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
                ExtractPath(inputPath, options.OutputPath, options.Info, options.Debug);
            }
        }

        /// <summary>
        /// Wrapper to extract data for a single path
        /// </summary>
        /// <param name="path">File or directory path</param>
        /// <param name="outputDirectory">Output directory path</param>
        /// <param name="includeDebug">Enable including debug information</param>
        private static void ExtractPath(string path, string outputDirectory, bool includeInfo, bool includeDebug)
        {
            // Normalize by getting the full path
            path = Path.GetFullPath(path);
            Console.WriteLine($"Checking possible path: {path}");

            // Check if the file or directory exists
            if (File.Exists(path))
            {
                ExtractFile(path, outputDirectory, includeInfo, includeDebug);
            }
            else if (Directory.Exists(path))
            {
                foreach (string file in IOExtensions.SafeEnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    ExtractFile(file, outputDirectory, includeInfo, includeDebug);
                }
            }
            else
            {
                Console.WriteLine($"{path} does not exist, skipping...");
            }
        }

        /// <summary>
        /// Wrapper to extract data for a single file
        /// </summary>
        /// <param name="file">File path</param>
        /// <param name="outputDirectory">Output directory path</param>
        /// <param name="includeDebug">Enable including debug information</param>
        private static void ExtractFile(string file, string outputDirectory, bool includeInfo, bool includeDebug)
        {
            // Attempt to print information
            if (includeInfo)
                PrintFileInfo(file, includeDebug);

            // Attempt to extract the file
            if (WiseOverlayHeader.ExtractAll(file, outputDirectory, includeDebug))
                Console.WriteLine($"Extracted {file} to {outputDirectory}");
            else
                Console.WriteLine(value: $"Failed to extract {file}!");
        }

        /// <summary>
        /// Wrapper to print overlay and script information for a single file
        /// </summary>
        /// <param name="file">File path</param>
        /// <param name="includeDebug">Enable including debug information</param>
        /// TODO: Implement script file printing?
        private static void PrintFileInfo(string file, bool includeDebug)
        {
            // Get the base info output name
            string filenameBase = $"info-{DateTime.Now:yyyy-MM-dd_HHmmss.ffff}";

            Console.WriteLine($"Attempting to print info for {file}");

            try
            {
                using Stream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // Try to find the overlay header
                if (!WiseOverlayHeader.FindOverlayHeader(stream, includeDebug, out var header) || header == null)
                {
                    Console.WriteLine($"No valid header could be found in {file}, skipping...");
                    return;
                }

                // Create the output data
                var builder = header.ExportStringBuilder();
                if (builder == null)
                {
                    Console.WriteLine("No item information could be generated");
                    return;
                }

                Console.WriteLine(builder);
                using var sw = new StreamWriter(File.OpenWrite($"{filenameBase}.txt"));
                sw.WriteLine(builder.ToString());
                sw.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(includeDebug ? ex : "[Exception opening file, please try again]");
                Console.WriteLine();
            }
        }
    }
}
