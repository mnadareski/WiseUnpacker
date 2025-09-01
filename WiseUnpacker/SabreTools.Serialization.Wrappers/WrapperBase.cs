using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.IO.Streams;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Wrappers
{
    public abstract class WrapperBase2 : IWrapper
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
        protected WrapperBase2(byte[]? data, int offset)
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
        protected WrapperBase2(Stream? data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.CanSeek || !data.CanRead)
                throw new ArgumentOutOfRangeException(nameof(data));

            _dataSource = new ViewStream(data, data.Position, data.Length - data.Position);
        }

        #endregion

        // TODO: This entire section will be replaced when IO updates
        #region Data

        /// <summary>
        /// Read data from the source
        /// </summary>
        /// <param name="position">Position in the source to read from</param>
        /// <param name="length">Length of the requested data</param>
        /// <returns>Byte array containing the requested data, null on error</returns>
        /// TODO: This will be replaced with the ReadFrom extension method in IO
        public byte[]? ReadFromDataSource(int position, int length)
        {
            // Validate the requested segment
            if (!_dataSource.SegmentValid(position, length))
                return null;

            try
            {
                long currentLocation = _dataSource.Position;

                _dataSource.Seek(position, SeekOrigin.Begin);
                byte[] sectionData = _dataSource.ReadBytes(length);
                _dataSource.Seek(currentLocation, SeekOrigin.Begin);

                return sectionData;

            }
            catch
            {
                // Absorb the error
                return null;
            }
        }

        /// <summary>
        /// Read string data from the source
        /// </summary>
        /// <param name="position">Position in the source to read from</param>
        /// <param name="length">Length of the requested data</param>
        /// <param name="charLimit">Number of characters needed to be a valid string</param>
        /// <returns>String list containing the requested data, null on error</returns>
        /// TODO: Remove when IO updated
        public List<string>? ReadStringsFromDataSource(int position, int length, int charLimit = 5)
        {
            // Read the data as a byte array first
            byte[]? sourceData = ReadFromDataSource(position, length);
            if (sourceData == null)
                return null;

            // Check for ASCII strings
            var asciiStrings = ReadStringsWithEncoding(sourceData, charLimit, Encoding.ASCII);

            // Check for UTF-8 strings
            // We are limiting the check for Unicode characters with a second byte of 0x00 for now
            var utf8Strings = ReadStringsWithEncoding(sourceData, charLimit, Encoding.UTF8);

            // Check for Unicode strings
            // We are limiting the check for Unicode characters with a second byte of 0x00 for now
            var unicodeStrings = ReadStringsWithEncoding(sourceData, charLimit, Encoding.Unicode);

            // Ignore duplicate strings across encodings
            List<string> sourceStrings = [.. asciiStrings, .. utf8Strings, .. unicodeStrings];

            // Sort the strings and return
            sourceStrings.Sort();
            return sourceStrings;
        }

        /// <summary>
        /// Read string data from the source with an encoding
        /// </summary>
        /// <param name="sourceData">Byte array representing the source data</param>
        /// <param name="charLimit">Number of characters needed to be a valid string</param>
        /// <param name="encoding">Character encoding to use for checking</param>
        /// <returns>String list containing the requested data, empty on error</returns>
        /// TODO: Remove when IO updated
#if NET20
        private static List<string> ReadStringsWithEncoding(byte[] sourceData, int charLimit, Encoding encoding)
#else
        private static HashSet<string> ReadStringsWithEncoding(byte[] sourceData, int charLimit, Encoding encoding)
#endif
        {
            // Constant from IO
            const int MaximumCharactersInString = 64;

            if (sourceData == null || sourceData.Length == 0)
                return [];
            if (charLimit <= 0 || charLimit > sourceData.Length)
                return [];

            // Create the string set to return
#if NET20
            var strings = new List<string>();
#else
            var strings = new HashSet<string>();
#endif

            // Check for strings
            int index = 0;
            while (index < sourceData.Length)
            {
                // Get the maximum number of characters
                int maxChars = encoding.GetMaxCharCount(sourceData.Length - index);
                int maxBytes = encoding.GetMaxByteCount(Math.Min(MaximumCharactersInString, maxChars));

                // Read the longest string allowed
                int maxRead = Math.Min(maxBytes, sourceData.Length - index);
                string temp = encoding.GetString(sourceData, index, maxRead);
                char[] tempArr = temp.ToCharArray();

                // Ignore empty strings
                if (temp.Length == 0)
                {
                    index++;
                    continue;
                }

                // Find the first instance of a control character
                int endOfString = Array.FindIndex(tempArr, c => char.IsControl(c) || (c & 0xFF00) != 0);
                if (endOfString > -1)
                    temp = temp.Substring(0, endOfString);

                // Otherwise, just add the string if long enough
                if (temp.Length >= charLimit)
                    strings.Add(temp);

                // Increment and continue
                index += Math.Max(encoding.GetByteCount(temp), 1);
            }

            return strings;
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
