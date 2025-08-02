using System.IO;
using SabreTools.Models.WiseInstaller;

namespace SabreTools.Serialization.Wrappers
{
    public class WiseScript : WrapperBase<ScriptFile>
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Wise Installer Script File";

        #endregion

        #region Extension Properties

        /// <inheritdoc cref="ScriptFile.States"/>
        public MachineState[]? States => Model.States;

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public WiseScript(ScriptFile? model, byte[]? data, int offset)
            : base(model, data, offset)
        {
            // All logic is handled by the base class
        }

        /// <inheritdoc/>
        public WiseScript(ScriptFile? model, Stream? data)
            : base(model, data)
        {
            // All logic is handled by the base class
        }

        /// <summary>
        /// Create a Wise installer script file from a byte array and offset
        /// </summary>
        /// <param name="data">Byte array representing the script</param>
        /// <param name="offset">Offset within the array to parse</param>
        /// <returns>A Wise installer script file wrapper on success, null on failure</returns>
        public static WiseScript? Create(byte[]? data, int offset)
        {
            // If the data is invalid
            if (data == null || data.Length == 0)
                return null;

            // If the offset is out of bounds
            if (offset < 0 || offset >= data.Length)
                return null;

            // Create a memory stream and use that
            var dataStream = new MemoryStream(data, offset, data.Length - offset);
            return Create(dataStream);
        }

        /// <summary>
        /// Create a Wise installer script file from a Stream
        /// </summary>
        /// <param name="data">Stream representing the script</param>
        /// <returns>A Wise installer script file wrapper on success, null on failure</returns>
        public static WiseScript? Create(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                var mkb = Deserializers.WiseScript.DeserializeStream(data);
                if (mkb == null)
                    return null;

                return new WiseScript(mkb, data);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
