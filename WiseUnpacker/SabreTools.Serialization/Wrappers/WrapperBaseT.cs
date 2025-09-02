using System;
using System.IO;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Wrappers
{
    public abstract class WrapperBase<T> : WrapperBase, IWrapper<T>
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
        protected WrapperBase(T? model, byte[]? data, int offset)
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
        protected WrapperBase(T? model, Stream? data)
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
