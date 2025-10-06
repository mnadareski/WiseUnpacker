using System;
using System.Collections.Generic;
using System.IO;

namespace WiseUnpacker
{
    /// <summary>
    /// Set of options for the executable
    /// </summary>
    internal sealed class Options
    {
        #region Properties

        /// <summary>
        /// Enable debug output for relevant operations
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Enable extraction for the input file
        /// </summary>
        public bool Extract { get; set; }

        /// <summary>
        /// Output information to file only, skip printing to console
        /// </summary>
        public bool FileOnly { get; set; }

        /// <summary>
        /// Print both the overlay and script information
        /// to screen and file, if possible
        /// </summary>
        public bool Info { get; set; }

#if NETCOREAPP
        /// <summary>
        /// Enable JSON output
        /// </summary>
        public bool Json { get; set; }
#endif

        /// <summary>
        /// Set of input paths to use for operations
        /// </summary>
        public List<string> InputPaths { get; private set; } = [];

        /// <summary>
        /// Print per-file statistics information
        /// </summary>
        public bool PerFile { get; set; }

        /// <summary>
        /// Output path for archive extraction
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;

        #endregion

        /// <summary>
        /// Display help text
        /// </summary>
        public static void DisplayHelp()
        {
            Console.WriteLine("Wise Installer Reference Implementation");
            Console.WriteLine();
            Console.WriteLine("WiseUnpacker <options> file|directory ...");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("-?, -h, --help           Display this help text and quit");
            Console.WriteLine("-d, --debug              Enable debug mode");
            Console.WriteLine("-i, --info               Print overlay and script info");
#if NETCOREAPP
            Console.WriteLine("-j, --json               Print info as JSON (requires --info)");
#endif
            Console.WriteLine("-f, --file               Print to file only (requires --info)");
            Console.WriteLine("-p, --per-file           Print per-file statistics (requires --info)");
            Console.WriteLine("-x, --extract            Extract files (default if nothing else provided)");
            Console.WriteLine("-o, --outdir [PATH]      Set output path for extraction (required)");
        }

        /// <summary>
        /// Validate the extraction path
        /// </summary>
        public bool ValidateExtractionPath()
        {
            // Null or empty output path
            if (string.IsNullOrEmpty(OutputPath))
            {
                Console.WriteLine("Output directory required for extraction!");
                Console.WriteLine();
                return false;
            }

            // Malformed output path or invalid location
            try
            {
                OutputPath = Path.GetFullPath(OutputPath);
                Directory.CreateDirectory(OutputPath);
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