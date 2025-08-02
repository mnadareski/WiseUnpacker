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

            // Get the input path and generate a default output
            string input = args[0];
            string? outdir = CreateOutdir(input);
            if (string.IsNullOrEmpty(outdir))
            {
                DisplayHelp("Could not determine output path");
                return;
            }

            // Use the provided output directory, if possible
            if (args.Length > 1)
                outdir = Path.GetFullPath(args[1]);

            // Create a new unpacker
            var unpacker = new Unpacker(input);

            // Attempt to extract the file
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

            Console.WriteLine(@"Usage: Test <input> [output]

<input> is required and must be the path to the input file.
Only one path can be specified at a time.

[output] is optional and represents an output folder for extracted files.
If not specified, a folder will be generated next to the input file.");
        }

        /// <summary>
        /// Generate the output directory path, if possible
        /// </summary>
        /// <param name="input">Input path to generate from</param>
        /// <returns>Output directory path on success, null on error</returns>
        private static string? CreateOutdir(string input)
        {
            // If the file path is not valid
            if (string.IsNullOrEmpty(input) || !File.Exists(input))
                return null;

            // Get the full path for the input, if possible
            input = Path.GetFullPath(input);

            // Get the directory name and filename without extension for processing
            string? directoryName = Path.GetDirectoryName(input);
            string? fileNameWithoutExtension = Path.GetFileNameWithoutExtension(input);

            // Return an output path based on the two parts
            if (string.IsNullOrEmpty(directoryName) && string.IsNullOrEmpty(fileNameWithoutExtension))
                return null;
            else if (string.IsNullOrEmpty(directoryName) && !string.IsNullOrEmpty(fileNameWithoutExtension))
                return fileNameWithoutExtension;
            else if (!string.IsNullOrEmpty(directoryName) && string.IsNullOrEmpty(fileNameWithoutExtension))
                return directoryName;
            else
                return Path.Combine(directoryName!, fileNameWithoutExtension!);
        }
    }
}
