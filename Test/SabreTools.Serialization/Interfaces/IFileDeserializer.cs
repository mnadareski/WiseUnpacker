namespace SabreTools.Serialization.Interfaces
{
    /// <summary>
    /// Defines how to deserialize from files
    /// </summary>
    public interface IFileDeserializer<T>
    {
        /// <summary>
        /// Deserialize a file into <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Type of object to deserialize to</typeparam>
        /// <param name="path">Path to deserialize from</param>
        /// <returns>Filled object on success, null on error</returns>
        T? Deserialize(string? path);
    }
}
