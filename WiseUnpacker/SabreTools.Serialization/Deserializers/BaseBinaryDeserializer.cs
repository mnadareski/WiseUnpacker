using System;
using System.IO;
using System.Reflection;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Deserializers
{
    /// <summary>
    /// Base class for all binary deserializers
    /// </summary>
    /// <typeparam name="TModel">Type of the model to deserialize</typeparam>
    /// <remarks>These methods assume there is a concrete implementation of the deserialzier for the model available</remarks>
    public abstract class BaseBinaryDeserializer<TModel> :
        IByteDeserializer<TModel>,
        IFileDeserializer<TModel>,
        IStreamDeserializer<TModel>
    {
        /// <summary>
        /// Indicates if compressed files should be decompressed before processing
        /// </summary>
        protected virtual bool SkipCompression => false;

        #region IByteDeserializer

        /// <inheritdoc/>
        public virtual TModel? Deserialize(byte[]? data, int offset)
        {
            // If the data is invalid
            if (data == null || data.Length == 0)
                return default;

            // If the offset is out of bounds
            if (offset < 0 || offset >= data.Length)
                return default;

            // Create a memory stream and parse that
            var dataStream = new MemoryStream(data, offset, data.Length - offset);
            return DeserializeStream(dataStream);
        }

        #endregion

        #region IFileDeserializer

        /// <inheritdoc/>
        public virtual TModel? Deserialize(string? path)
        {
            using var stream = PathProcessor.OpenStream(path, SkipCompression);
            return DeserializeStream(stream);
        }

        #endregion

        #region IStreamDeserializer

        /// <inheritdoc/>
        public abstract TModel? Deserialize(Stream? data);

        #endregion

        #region Static Implementations

        /// <inheritdoc cref="IByteDeserializer.Deserialize(byte[]?, int)"/>
        public static TModel? DeserializeBytes(byte[]? data, int offset)
        {
            var deserializer = GetType<IByteDeserializer<TModel>>();
            if (deserializer == null)
                return default;

            return deserializer.Deserialize(data, offset);
        }

        /// <inheritdoc cref="IFileDeserializer.Deserialize(string?)"/>
        public static TModel? DeserializeFile(string? path)
        {
            var deserializer = GetType<IFileDeserializer<TModel>>();
            if (deserializer == null)
                return default;

            return deserializer.Deserialize(path);
        }

        /// <inheritdoc cref="IStreamDeserializer.Deserialize(Stream?)"/>
        public static TModel? DeserializeStream(Stream? data)
        {
            var deserializer = GetType<IStreamDeserializer<TModel>>();
            if (deserializer == null)
                return default;

            return deserializer.Deserialize(data);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Get a constructed instance of a type, if possible
        /// </summary>
        /// <typeparam name="TDeserializer">Deserializer type to construct</typeparam>
        /// <returns>Deserializer of the requested type, null on error</returns>
        private static TDeserializer? GetType<TDeserializer>()
        {
            // If the deserializer type is invalid
            string? deserializerName = typeof(TDeserializer)?.Name;
            if (deserializerName == null)
                return default;

            // If the deserializer has no generic arguments
            var genericArgs = typeof(TDeserializer).GetGenericArguments();
            if (genericArgs.Length == 0)
                return default;

            // Loop through all loaded assemblies
            Type modelType = genericArgs[0];
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // If the assembly is invalid
                if (assembly == null)
                    return default;

                // If not all types can be loaded, use the ones that could be
                Type?[] assemblyTypes = [];
                try
                {
                    assemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    assemblyTypes = rtle.Types ?? [];
                }

                // Loop through all types 
                foreach (Type? type in assemblyTypes)
                {
                    // If the type is invalid
                    if (type == null)
                        continue;

                    // If the type isn't a class
                    if (!type.IsClass)
                        continue;

                    // If the type doesn't implement the interface
                    var interfaceType = type.GetInterface(deserializerName);
                    if (interfaceType == null)
                        continue;

                    // If the interface doesn't use the correct type parameter
                    var genericTypes = interfaceType.GetGenericArguments();
                    if (genericTypes.Length != 1 || genericTypes[0] != modelType)
                        continue;

                    // Try to create a concrete instance of the type
                    var instance = (TDeserializer?)Activator.CreateInstance(type);
                    if (instance != null)
                        return instance;
                }
            }

            return default;
        }

        #endregion
    }
}
