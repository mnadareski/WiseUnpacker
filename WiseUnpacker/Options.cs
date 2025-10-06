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