namespace SabreTools.Serialization.Interfaces
{
    /// <summary>
    /// Represents a wrapper around a top-level model
    /// </summary>
    public interface IWrapper
    {
        /// <summary>
        /// Get a human-readable description of the wrapper
        /// </summary>
        string Description();

#if NETCOREAPP
        /// <summary>
        /// Export the item information as JSON
        /// </summary>
        string ExportJSON();
#endif
    }
}
