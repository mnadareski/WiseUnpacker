namespace WiseUnpacker
{
    /// <summary>
    /// Represents a single WISE unpacker
    /// </summary>
    public interface IWiseUnpacker
    {
        // <summary>
        /// Attempt to parse, extract, and rename all files from a WISE installer
        /// </summary>
        /// <param name="outputPath">Output directory for extracted files</param>
        /// <returns>True if extraction was a success, false otherwise</returns>
        bool Run(string outputPath);
    }
}