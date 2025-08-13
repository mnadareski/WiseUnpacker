using System;
using System.Collections.Generic;
using System.IO;

namespace Test
{
    /// <summary>
    /// Set of options for the test executable
    /// </summary>
    internal sealed class Options
    {
        #region Properties

        /// <summary>
        /// Enable debug output for relevant operations
        /// </summary>
        public bool Debug { get; private set; } = false;

        /// <summary>
        /// Enable extraction for the input file
        /// </summary>
        public bool Extract { get; private set; } = false;

        /// <summary>
        /// Output information to file only, skip printing to console
        /// </summary>
        public bool FileOnly { get; private set; } = false;

        /// <summary>
        /// Print both the overlay and script information
        /// to screen and file, if possible
        /// </summary>
        public bool Info { get; private set; } = false;

#if NETCOREAPP
        /// <summary>
        /// Enable JSON output
        /// </summary>
        public bool Json { get; private set; } = false;
#endif

        /// <summary>
        /// Set of input paths to use for operations
        /// </summary>
        public List<string> InputPaths { get; private set; } = [];

        /// <summary>
        /// Output path for archive extraction
        /// </summary>
        public string OutputPath { get; private set; } = string.Empty;

        #endregion

        /// <summary>
        /// Parse commandline arguments into an Options object
        /// </summary>
        public static Options? ParseOptions(string[] args)
        {
            // If we have invalid arguments
            if (args == null || args.Length == 0)
                return null;

            // Create an Options object
            var options = new Options();

            // Parse the options and paths
            for (int index = 0; index < args.Length; index++)
            {
                string arg = args[index];
                switch (arg)
                {
                    case "-?":
                    case "-h":
                    case "--help":
                        return null;

                    case "-d":
                    case "--debug":
                        options.Debug = true;
                        break;

                    case "-i":
                    case "--info":
                        options.Info = true;
                        break;

                    case "-f":
                    case "--file":
                        options.FileOnly = true;
                        break;

                    case "-j":
                    case "--json":
#if NETCOREAPP
                        options.Json = true;
#else
                        Console.WriteLine("JSON output not available in .NET Framework");
#endif
                        break;

                    case "-o":
                    case "--outdir":
                        options.OutputPath = index + 1 < args.Length ? args[++index] : string.Empty;
                        break;

                    case "-x":
                    case "--extract":
                        options.Extract = true;
                        break;

                    default:
                        options.InputPaths.Add(arg);
                        break;
                }
            }

            // If neither info nor extract is defined, default to extract
            if (!options.Info && !options.Extract)
                options.Extract = true;

            // Validate we have any input paths to work on
            if (options.InputPaths.Count == 0)
            {
                Console.WriteLine("At least one path is required!");
                return null;
            }

            // Validate the output path
            if (options.Extract)
            {
                bool validPath = ValidateExtractionPath(options);
                if (!validPath)
                    return null;
            }

            return options;
        }

        /// <summary>
        /// Display help text
        /// </summary>
        public static void DisplayHelp()
        {
            Console.WriteLine("Wise Installer Reference Implementation");
            Console.WriteLine();
            Console.WriteLine("Test <options> file|directory ...");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("-?, -h, --help           Display this help text and quit");
            Console.WriteLine("-d, --debug              Enable debug mode");
            Console.WriteLine("-i, --info               Print overlay and script info");
            Console.WriteLine("-f, --file               Print to file only");
#if NETCOREAPP
            Console.WriteLine("-j, --json               Print info as JSON");
#endif
            Console.WriteLine("-x, --extract            Extract files (default if nothing else provided)");
            Console.WriteLine("-o, --outdir [PATH]      Set output path for extraction (required)");
        }

        /// <summary>
        /// Validate the extraction path
        /// </summary>
        private static bool ValidateExtractionPath(Options options)
        {
            // Null or empty output path
            if (string.IsNullOrEmpty(options.OutputPath))
            {
                Console.WriteLine("Output directory required for extraction!");
                Console.WriteLine();
                return false;
            }

            // Malformed output path or invalid location
            try
            {
                options.OutputPath = Path.GetFullPath(options.OutputPath);
                Directory.CreateDirectory(options.OutputPath);
            }
            catch
            {
                Console.WriteLine("Output directory could not be created!");
                Console.WriteLine();
                return false;
            }

            return true;
        }
    }
}