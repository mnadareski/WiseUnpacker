namespace WiseUnpacker
{
    public class WiseUnpacker
    {
        /// <summary>
        /// Create a new heuristic unpacker
        /// </summary>
        public WiseUnpacker() { }

        /// <summary>
        /// Attempt to extract a Wise installer
        /// </summary>
        /// <param name="file">Path to the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public bool ExtractTo(string file, string outputPath)
        {
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
        /// Attempt to extract a Wise installer using HWUN
        /// </summary>
        /// <param name="file">Path to the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <param name="options">HWUN-compatible options string (optional)</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public bool ExtractToHWUN(string file, string outputPath, string? options = null)
        {
            var hwun = new HWUN.Unpacker(file, options);
            return hwun.Run(outputPath);
        }

        /// <summary>
        /// Attempt to extract a Wise installer using E_WISE
        /// </summary>
        /// <param name="file">Path to the possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        public bool ExtractToEWISE(string file, string outputPath)
        {
            var ewise = new EWISE.Unpacker(file);
            return ewise.Run(outputPath);
        }
    }
}
