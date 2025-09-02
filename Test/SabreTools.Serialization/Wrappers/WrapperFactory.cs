using System;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.Matching;
using SabreTools.Serialization.Interfaces;
using static SabreTools.Matching.Extensions;

namespace SabreTools.Serialization.Wrappers
{
    public static class WrapperFactory
    {
        /// <summary>
        /// Create an instance of a wrapper based on the executable type
        /// </summary>
        /// <param name="stream">Stream data to parse</param>
        /// <returns>IWrapper representing the executable, null on error</returns>
        public static IWrapper? CreateExecutableWrapper(Stream? stream)
        {
            // If we have no stream
            if (stream == null)
                return null;

            // Cache the current offset
            long initialOffset = stream.Position;

            // Try to get an MS-DOS wrapper first
            var wrapper = MSDOS.Create(stream);
            if (wrapper == null || wrapper is not MSDOS msdos)
                return null;

            // Check for a valid new executable address
            if (msdos.Model.Header?.NewExeHeaderAddr == null || initialOffset + msdos.Model.Header.NewExeHeaderAddr >= stream.Length)
                return wrapper;

            // Try to read the executable info
            stream.Seek(initialOffset + msdos.Model.Header.NewExeHeaderAddr, SeekOrigin.Begin);
            var magic = stream.ReadBytes(4);

            // If we didn't get valid data at the offset
            if (magic == null)
            {
                return wrapper;
            }

            // New Executable
            else if (magic.StartsWith(Models.NewExecutable.Constants.SignatureBytes))
            {
                stream.Seek(initialOffset, SeekOrigin.Begin);
                return NewExecutable.Create(stream);
            }

            // Portable Executable
            else if (magic.StartsWith(Models.PortableExecutable.Constants.SignatureBytes))
            {
                stream.Seek(initialOffset, SeekOrigin.Begin);
                return PortableExecutable.Create(stream);
            }

            // Everything else fails
            return null;
        }
    }
}
