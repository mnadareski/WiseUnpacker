namespace SabreTools.Serialization.Interfaces
{
    /// <summary>
    /// Represents an item that is extractable
    /// </summary>
    public interface IExtractable
    {
        /// <summary>
        /// Extract to an output directory
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        bool Extract(string outputDirectory, bool includeDebug);
    }
}
