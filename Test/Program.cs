using System;
using System.IO;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            // Valide the arguments
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("One input file path required");
                return;
            }

            // Get the input path and generate a default output
            string input = args[0];
            string? outdir = CreateOutdir(input);
            if (string.IsNullOrEmpty(outdir))
            {
                Console.WriteLine("Could not determine output path");
                return;
            }

            // Use the provided output directory, if possible
            if (args.Length > 1)
                outdir = Path.GetFullPath(args[1]);

            // Attempt to extract the file
            var unpacker = new WiseUnpacker.WiseUnpacker();
            if (unpacker.ExtractTo(input, outdir!))
                Console.WriteLine($"Extracted {input} to {outdir}");
            else
                Console.WriteLine(value: $"Failed to extract {input}!");

            // Handle redirected inputs
#if NET452_OR_GREATER || NETCOREAPP
            if (!Console.IsInputRedirected)
#endif
                Console.ReadKey();
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
