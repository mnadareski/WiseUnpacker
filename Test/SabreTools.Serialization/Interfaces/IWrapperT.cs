namespace SabreTools.Serialization.Interfaces
{
    /// <summary>
    /// Represents a wrapper around a top-level model
    /// </summary>
    /// <typeparam name="TModel">Top-level model for the wrapper</typeparam>
    public interface IWrapper<TModel> : IWrapper
    {
        /// <summary>
        /// Get the backing model
        /// </summary>
        TModel GetModel();
    }
}
