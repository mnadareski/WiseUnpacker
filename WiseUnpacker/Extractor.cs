using System.IO;

namespace WiseUnpacker
{
    public static class Extractor
    {
        /// <summary>
        /// Attempt to extract a Wise installer
        /// </summary>
        /// <param name="file">Path to the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public static bool ExtractTo(string file, string outputPath)
        {
            // Use naive parsing code
            if (ExtractToNaive(file, outputPath))
                return true;

            // Use E_WISE-derived code
            if (ExtractToEWISE(file, outputPath))
                return true;

            // Use HWUN-derived code
            if (ExtractToHWUN(file, outputPath))
                return true;

            // Everything failed
            return false;
        }

        /// <summary>
        /// Attempt to extract a Wise installer
        /// </summary>
        /// <param name="stream">Stream representing the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public static bool ExtractTo(Stream stream, string outputPath)
        {
            // Use naive parsing code
            if (ExtractToNaive(stream, outputPath))
                return true;

            // Use E_WISE-derived code
            if (ExtractToEWISE(stream, outputPath))
                return true;

            // Use HWUN-derived code
            if (ExtractToHWUN(stream, outputPath))
                return true;

            // Everything failed
            return false;
        }

        /// <summary>
        /// Attempt to extract a Wise installer using E_WISE
        /// </summary>
        /// <param name="file">Path to the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public static bool ExtractToEWISE(string file, string outputPath)
        {
            var ewise = new EWISE.Unpacker(file);
            return ewise.Run(outputPath);
        }

        /// <summary>
        /// Attempt to extract a Wise installer using E_WISE
        /// </summary>
        /// <param name="stream">Stream representing the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public static bool ExtractToEWISE(Stream stream, string outputPath)
        {
            var ewise = new EWISE.Unpacker(stream);
            return ewise.Run(outputPath);
        }

        /// <summary>
        /// Attempt to extract a Wise installer using HWUN
        /// </summary>
        /// <param name="file">Path to the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <param name="options">HWUN-compatible options string (optional)</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public static bool ExtractToHWUN(string file, string outputPath, string? options = null)
        {
            var hwun = new HWUN.Unpacker(file, options);
            return hwun.Run(outputPath);
        }

        /// <summary>
        /// Attempt to extract a Wise installer using HWUN
        /// </summary>
        /// <param name="stream">Stream representing the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <param name="options">HWUN-compatible options string (optional)</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public static bool ExtractToHWUN(Stream stream, string outputPath, string? options = null)
        {
            var hwun = new HWUN.Unpacker(stream, options);
            return hwun.Run(outputPath);
        }

        /// <summary>
        /// Attempt to extract a Wise installer using naive strategies
        /// </summary>
        /// <param name="file">Path to the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public static bool ExtractToNaive(string file, string outputPath)
        {
            var naive = new Naive.Unpacker(file);
            return naive.Run(outputPath);
        }

        /// <summary>
        /// Attempt to extract a Wise installer using naive strategies
        /// </summary>
        /// <param name="stream">Stream representing the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public static bool ExtractToNaive(Stream stream, string outputPath)
        {
            var naive = new Naive.Unpacker(stream);
            return naive.Run(outputPath);
        }
    }
}
