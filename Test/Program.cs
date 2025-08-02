using System;
using System.IO;
using WiseUnpacker;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            // Valide the arguments
            if (args == null || args.Length == 0)
            {
                DisplayHelp("One input file path required");
                return;
            }

            // Get the input path and optional output directory
            string input = args[0];
            string? outdir = args.Length > 1 ? args[1] : null;

            // Generate an output directory
            outdir = GenerateOutdir(input, outdir);
            if (string.IsNullOrEmpty(outdir))
            {
                DisplayHelp("Could not determine output path");
                return;
            }

            // Attempt to extract the file
            var unpacker = new Unpacker(input);
            if (unpacker.Run(outdir!))
                Console.WriteLine($"Extracted {input} to {outdir}");
            else
                Console.WriteLine(value: $"Failed to extract {input}!");
        }

        /// <summary>
        /// Display a basic help text
        /// </summary>
        /// <param name="err">Additional error text to display, can be null to ignore</param>
        private static void DisplayHelp(string? err = null)
        {
            if (!string.IsNullOrEmpty(err))
                Console.WriteLine($"Error: {err}");

            Console.WriteLine("Usage: Test <input> [output]");
            Console.WriteLine();
            Console.WriteLine("<input> is required and must be the path to the input file.");
            Console.WriteLine("Only one path can be specified at a time.");
            Console.WriteLine();
            Console.WriteLine("[output] is optional and represents an output folder for extracted files.");
            Console.WriteLine("If not specified, a folder will be generated next to the input file.");
        }

        /// <summary>
        /// Generate the output directory path, if possible
        /// </summary>
        /// <param name="input">Input path to generate from</param>
        /// <param name="outdir">User provided output directory</param>
        /// <returns>Output directory path on success, null on error</returns>
        private static string? GenerateOutdir(string input, string? outdir)
        {
            // If the file path is not valid
            if (string.IsNullOrEmpty(input) || !File.Exists(input))
                return null;

            // Use the provided output directory, if possible
            if (outdir != null)
                return Path.GetFullPath(outdir);

            // Get the full path for the input, if possible
            input = Path.GetFullPath(input);

            // Get the directory name and filename without extension for processing
            string? directoryName = Path.GetDirectoryName(input);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(input);

            // Return an output path based on the two parts
            outdir = fileNameWithoutExtension;
            if (!string.IsNullOrEmpty(directoryName))
                outdir = Path.Combine(directoryName, outdir);

            return outdir;
        }
    }
}
