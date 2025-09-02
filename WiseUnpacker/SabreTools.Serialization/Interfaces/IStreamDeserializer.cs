namespace SabreTools.Serialization.Interfaces
{
    /// <summary>
    /// Defines how to serialize from Streams
    /// </summary>
    public interface IStreamDeserializer<T>
    {
        /// <summary>
        /// Deserialize a Stream into <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Type of object to deserialize to</typeparam>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled object on success, null on error</returns>
        T? Deserialize(System.IO.Stream? data);
    }
}
