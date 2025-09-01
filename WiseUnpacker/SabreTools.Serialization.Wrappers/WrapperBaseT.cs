using System;
using System.IO;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Wrappers
{
    public abstract class WrapperBase2<T> : WrapperBase2, IWrapper<T>
    {
        #region Properties

        /// <inheritdoc/>
        public T GetModel() => Model;

        /// <summary>
        /// Internal model
        /// </summary>
        public T Model { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Construct a new instance of the wrapper from a byte array
        /// </summary>
        protected WrapperBase2(T? model, byte[]? data, int offset)
            : base(data, offset)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset >= data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            Model = model;
        }

        /// <summary>
        /// Construct a new instance of the wrapper from a Stream
        /// </summary>
        protected WrapperBase2(T? model, Stream? data)
            : base(data)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.CanSeek || !data.CanRead)
                throw new ArgumentOutOfRangeException(nameof(data));

            Model = model;
        }

        #endregion

        #region JSON Export

#if NETCOREAPP
        /// <inheritdoc/>
        public override string ExportJSON() => System.Text.Json.JsonSerializer.Serialize(Model, _jsonSerializerOptions);
#endif

        #endregion
    }
}
