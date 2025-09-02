using System;
using System.IO;
using SabreTools.IO.Streams;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Wrappers
{
    public abstract class WrapperBase : IWrapper
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public string Description() => DescriptionString;

        /// <summary>
        /// Description of the object
        /// </summary>
        public abstract string DescriptionString { get; }

        #endregion

        #region Properties

        /// <inheritdoc cref="ViewStream.Filename"/>
        public string? Filename => _dataSource.Filename;

        /// <inheritdoc cref="ViewStream.Length"/>
        public long Length => _dataSource.Length;

        #endregion

        #region Instance Variables

        /// <summary>
        /// Source of the original data
        /// </summary>
        protected readonly ViewStream _dataSource;

#if NETCOREAPP
        /// <summary>
        /// JSON serializer options for output printing
        /// </summary>
        protected System.Text.Json.JsonSerializerOptions _jsonSerializerOptions
        {
            get
            {
#if NETCOREAPP3_1
                var serializer = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
#else
                var serializer = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
#endif
                serializer.Converters.Add(new ConcreteAbstractSerializer());
                serializer.Converters.Add(new ConcreteInterfaceSerializer());
                serializer.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                return serializer;
            }
        }
#endif

        #endregion

        #region Constructors

        /// <summary>
        /// Construct a new instance of the wrapper from a byte array
        /// </summary>
        protected WrapperBase(byte[]? data, int offset)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset >= data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            _dataSource = new ViewStream(data, offset, data.Length - offset);
        }

        /// <summary>
        /// Construct a new instance of the wrapper from a Stream
        /// </summary>
        protected WrapperBase(Stream? data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.CanSeek || !data.CanRead)
                throw new ArgumentOutOfRangeException(nameof(data));

            _dataSource = new ViewStream(data, data.Position, data.Length - data.Position);
        }

        #endregion

        #region JSON Export

#if NETCOREAPP
        /// <summary>
        /// Export the item information as JSON
        /// </summary>
        public abstract string ExportJSON();
#endif

        #endregion
    }
}
