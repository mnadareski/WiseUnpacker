using System;
using System.IO;
using SabreTools.IO.Extensions;
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
                ExtractPath(inputPath, options.OutputPath, options.Debug);
            }

            
        }

        /// <summary>
        /// Wrapper to extract data for a single path
        /// </summary>
        /// <param name="path">File or directory path</param>
        /// <param name="outputDirectory">Output directory path</param>
        /// <param name="includeDebug">Enable including debug information</param>
        private static void ExtractPath(string path, string outputDirectory, bool includeDebug)
        {
            // Normalize by getting the full path
            path = Path.GetFullPath(path);
            Console.WriteLine($"Checking possible path: {path}");

            // Check if the file or directory exists
            if (File.Exists(path))
            {
                ExtractFile(path, outputDirectory, includeDebug);
            }
            else if (Directory.Exists(path))
            {
                foreach (string file in IOExtensions.SafeEnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    ExtractFile(file, outputDirectory, includeDebug);
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
        private static void ExtractFile(string file, string outputDirectory, bool includeDebug)
        {
            // Attempt to extract the file
            if (WiseOverlayHeader.ExtractAll(file, outputDirectory, includeDebug))
                Console.WriteLine($"Extracted {file} to {outputDirectory}");
            else
                Console.WriteLine(value: $"Failed to extract {file}!");
        }
    }
}
