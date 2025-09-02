using System;
using System.Collections.Generic;
using System.IO;
using SabreTools.IO.Extensions;
using SabreTools.Matching;
using SabreTools.Models.NewExecutable;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Wrappers
{
    public class NewExecutable : WrapperBase<Executable>, IExtractable
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "New Executable (NE)";

        #endregion

        #region Extension Properties

        /// <inheritdoc cref="Executable.Header"/>
        public ExecutableHeader? Header => Model.Header;

        /// <inheritdoc cref="Executable.ImportedNameTable"/>
        public Dictionary<ushort, ImportedNameTableEntry>? ImportedNameTable => Model.ImportedNameTable;

        /// <inheritdoc cref="Executable.NonResidentNameTable"/>
        public NonResidentNameTableEntry[]? NonResidentNameTable => Model.NonResidentNameTable;

        /// <summary>
        /// Address of the overlay, if it exists
        /// </summary>
        /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/exefile.c"/>
        public long OverlayAddress
        {
            get
            {
                lock (_sourceDataLock)
                {
                    // Use the cached data if possible
                    if (_overlayAddress != null)
                        return _overlayAddress.Value;

                    // Get the available source length, if possible
                    long dataLength = Length;
                    if (dataLength == -1)
                        return -1;

                    // If a required property is missing
                    if (Header == null || SegmentTable == null || ResourceTable?.ResourceTypes == null)
                        return -1;

                    // Search through the segments table to find the furthest
                    long endOfSectionData = -1;
                    foreach (var entry in SegmentTable)
                    {
                        // Get end of segment data
                        long offset = (entry.Offset * (1 << Header.SegmentAlignmentShiftCount)) + entry.Length;

                        // Read and find the end of the relocation data
#if NET20 || NET35
                        if ((entry.FlagWord & SegmentTableEntryFlag.RELOCINFO) != 0)
#else
                        if (entry.FlagWord.HasFlag(SegmentTableEntryFlag.RELOCINFO))
#endif
                        {
                            _dataSource.Seek(offset, SeekOrigin.Begin);
                            var relocationData = Deserializers.NewExecutable.ParsePerSegmentData(_dataSource);

                            offset = _dataSource.Position;
                        }

                        if (offset > endOfSectionData)
                            endOfSectionData = offset;
                    }

                    // Search through the resources table to find the furthest
                    foreach (var entry in ResourceTable.ResourceTypes)
                    {
                        // Skip invalid entries
                        if (entry.ResourceCount == 0 || entry.Resources == null || entry.Resources.Length == 0)
                            continue;

                        foreach (var resource in entry.Resources)
                        {
                            int offset = (resource.Offset << ResourceTable.AlignmentShiftCount) + resource.Length;
                            if (offset > endOfSectionData)
                                endOfSectionData = offset;
                        }
                    }

                    // If we didn't find the end of section data
                    if (endOfSectionData <= 0)
                        endOfSectionData = -1;

                    // Cache and return the position
                    _overlayAddress = endOfSectionData;
                    return _overlayAddress.Value;
                }
            }
        }

        /// <summary>
        /// Overlay data, if it exists
        /// </summary>
        /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/exefile.c"/>
        public byte[]? OverlayData
        {
            get
            {
                lock (_sourceDataLock)
                {
                    // Use the cached data if possible
                    if (_overlayData != null)
                        return _overlayData;

                    // Get the available source length, if possible
                    long dataLength = Length;
                    if (dataLength == -1)
                        return null;

                    // If a required property is missing
                    if (Header == null || SegmentTable == null || ResourceTable?.ResourceTypes == null)
                        return null;

                    // Search through the segments table to find the furthest
                    long endOfSectionData = -1;
                    foreach (var entry in SegmentTable)
                    {
                        // Get end of segment data
                        long offset = (entry.Offset * (1 << Header.SegmentAlignmentShiftCount)) + entry.Length;

                        // Read and find the end of the relocation data
#if NET20 || NET35
                        if ((entry.FlagWord & SegmentTableEntryFlag.RELOCINFO) != 0)
#else
                        if (entry.FlagWord.HasFlag(SegmentTableEntryFlag.RELOCINFO))
#endif
                        {
                            _dataSource.Seek(offset, SeekOrigin.Begin);
                            var relocationData = Deserializers.NewExecutable.ParsePerSegmentData(_dataSource);

                            offset = _dataSource.Position;
                        }

                        if (offset > endOfSectionData)
                            endOfSectionData = offset;
                    }

                    // Search through the resources table to find the furthest
                    foreach (var entry in ResourceTable.ResourceTypes)
                    {
                        // Skip invalid entries
                        if (entry.ResourceCount == 0 || entry.Resources == null || entry.Resources.Length == 0)
                            continue;

                        foreach (var resource in entry.Resources)
                        {
                            int offset = (resource.Offset << ResourceTable.AlignmentShiftCount) + resource.Length;
                            if (offset > endOfSectionData)
                                endOfSectionData = offset;
                        }
                    }

                    // If we didn't find the end of section data
                    if (endOfSectionData <= 0)
                        return null;

                    // If we're at the end of the file, cache an empty byte array
                    if (endOfSectionData >= dataLength)
                    {
                        _overlayData = [];
                        return _overlayData;
                    }

                    // Otherwise, cache and return the data
                    long overlayLength = dataLength - endOfSectionData;
                    _overlayData = _dataSource.ReadFrom((int)endOfSectionData, (int)overlayLength, retainPosition: true);
                    return _overlayData;
                }
            }
        }

        /// <summary>
        /// Overlay strings, if they exist
        /// </summary>
        public List<string>? OverlayStrings
        {
            get
            {
                lock (_sourceDataLock)
                {
                    // Use the cached data if possible
                    if (_overlayStrings != null)
                        return _overlayStrings;

                    // Get the available source length, if possible
                    long dataLength = Length;
                    if (dataLength == -1)
                        return null;

                    // If a required property is missing
                    if (Header == null || SegmentTable == null || ResourceTable?.ResourceTypes == null)
                        return null;

                    // Search through the segments table to find the furthest
                    int endOfSectionData = -1;
                    foreach (var entry in SegmentTable)
                    {
                        int offset = (entry.Offset << Header.SegmentAlignmentShiftCount) + entry.Length;
                        if (offset > endOfSectionData)
                            endOfSectionData = offset;
                    }

                    // Search through the resources table to find the furthest
                    foreach (var entry in ResourceTable.ResourceTypes)
                    {
                        // Skip invalid entries
                        if (entry.ResourceCount == 0 || entry.Resources == null || entry.Resources.Length == 0)
                            continue;

                        foreach (var resource in entry.Resources)
                        {
                            int offset = (resource.Offset << ResourceTable.AlignmentShiftCount) + resource.Length;
                            if (offset > endOfSectionData)
                                endOfSectionData = offset;
                        }
                    }

                    // If we didn't find the end of section data
                    if (endOfSectionData <= 0)
                        return null;

                    // Adjust the position of the data by 705 bytes
                    // TODO: Investigate what the byte data is
                    endOfSectionData += 705;

                    // If we're at the end of the file, cache an empty list
                    if (endOfSectionData >= dataLength)
                    {
                        _overlayStrings = [];
                        return _overlayStrings;
                    }

                    // TODO: Revisit the 16 MiB limit
                    // Cap the check for overlay strings to 16 MiB (arbitrary)
                    long overlayLength = Math.Min(dataLength - endOfSectionData, 16 * 1024 * 1024);

                    // Otherwise, cache and return the strings
                    _overlayStrings = _dataSource.ReadStringsFrom(endOfSectionData, (int)overlayLength, charLimit: 3);
                    return _overlayStrings;
                }
            }
        }

        /// <inheritdoc cref="Executable.ResidentNameTable"/>
        public ResidentNameTableEntry[]? ResidentNameTable => Model.ResidentNameTable;

        /// <inheritdoc cref="Executable.ResourceTable"/>
        public ResourceTable? ResourceTable => Model.ResourceTable;

        /// <inheritdoc cref="Executable.SegmentTable"/>
        public SegmentTableEntry[]? SegmentTable => Model.SegmentTable;

        /// <inheritdoc cref="Executable.Stub"/>
        public Models.MSDOS.Executable? Stub => Model.Stub;

        /// <summary>
        /// Stub executable data, if it exists
        /// </summary>
        public byte[]? StubExecutableData
        {
            get
            {
                lock (_sourceDataLock)
                {
                    // If we already have cached data, just use that immediately
                    if (_stubExecutableData != null)
                        return _stubExecutableData;

                    if (Stub?.Header?.NewExeHeaderAddr == null)
                        return null;

                    // Populate the raw stub executable data based on the source
                    int endOfStubHeader = 0x40;
                    int lengthOfStubExecutableData = (int)Stub.Header.NewExeHeaderAddr - endOfStubHeader;
                    _stubExecutableData = _dataSource.ReadFrom(endOfStubHeader, lengthOfStubExecutableData, retainPosition: true);

                    // Cache and return the stub executable data, even if null
                    return _stubExecutableData;
                }
            }
        }

        #endregion

        #region Instance Variables

        /// <summary>
        /// Address of the overlay, if it exists
        /// </summary>
        private long? _overlayAddress = null;

        /// <summary>
        /// Overlay data, if it exists
        /// </summary>
        private byte[]? _overlayData = null;

        /// <summary>
        /// Overlay strings, if they exist
        /// </summary>
        private List<string>? _overlayStrings = null;

        /// <summary>
        /// Stub executable data, if it exists
        /// </summary>
        private byte[]? _stubExecutableData = null;

        /// <summary>
        /// Lock object for reading from the source
        /// </summary>
        private readonly object _sourceDataLock = new();

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public NewExecutable(Executable? model, byte[]? data, int offset)
            : base(model, data, offset)
        {
            // All logic is handled by the base class
        }

        /// <inheritdoc/>
        public NewExecutable(Executable? model, Stream? data)
            : base(model, data)
        {
            // All logic is handled by the base class
        }

        /// <summary>
        /// Create an NE executable from a byte array and offset
        /// </summary>
        /// <param name="data">Byte array representing the executable</param>
        /// <param name="offset">Offset within the array to parse</param>
        /// <returns>An NE executable wrapper on success, null on failure</returns>
        public static NewExecutable? Create(byte[]? data, int offset)
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
        /// Create an NE executable from a Stream
        /// </summary>
        /// <param name="data">Stream representing the executable</param>
        /// <returns>An NE executable wrapper on success, null on failure</returns>
        public static NewExecutable? Create(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long currentOffset = data.Position;

                var model = Deserializers.NewExecutable.DeserializeStream(data);
                if (model == null)
                    return null;

                data.Seek(currentOffset, SeekOrigin.Begin);
                return new NewExecutable(model, data);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Extraction

        /// <inheritdoc/>
        /// <remarks>
        /// This extracts the following data:
        /// - Archives and executables in the overlay
        /// - Wise installers
        /// </remarks>
        public bool Extract(string outputDirectory, bool includeDebug)
        {
            bool overlay = ExtractFromOverlay(outputDirectory, includeDebug);
            bool wise = ExtractWise(outputDirectory, includeDebug);

            return overlay | wise;
        }

        /// <summary>
        /// Extract data from the overlay
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        public bool ExtractFromOverlay(string outputDirectory, bool includeDebug)
        {
            try
            {
                // Cache the overlay data for easier reading
                var overlayData = OverlayData;
                if (overlayData == null || overlayData.Length == 0)
                    return false;

                // Set the output variables
                int overlayOffset = 0;
                string extension = string.Empty;

                // Only process the overlay if it is recognized
                for (; overlayOffset < 0x100 && overlayOffset < overlayData.Length; overlayOffset++)
                {
                    int temp = overlayOffset;
                    byte[] overlaySample = overlayData.ReadBytes(ref temp, 0x10);

                    if (overlaySample.StartsWith([0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C]))
                    {
                        extension = "7z";
                        break;
                    }
                    else if (overlaySample.StartsWith([0x3B, 0x21, 0x40, 0x49, 0x6E, 0x73, 0x74, 0x61, 0x6C, 0x6C]))
                    {
                        // 7-zip SFX script -- ";!@Install" to ";!@InstallEnd@!"
                        overlayOffset = overlayData.FirstPosition([0x3B, 0x21, 0x40, 0x49, 0x6E, 0x73, 0x74, 0x61, 0x6C, 0x6C, 0x45, 0x6E, 0x64, 0x40, 0x21]);
                        if (overlayOffset == -1)
                            return false;

                        overlayOffset += 15;
                        extension = "7z";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.MicrosoftCabinet.Constants.SignatureBytes))
                    {
                        extension = "cab";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.PKZIP.Constants.LocalFileHeaderSignatureBytes))
                    {
                        extension = "zip";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.PKZIP.Constants.EndOfCentralDirectoryRecordSignatureBytes))
                    {
                        extension = "zip";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.PKZIP.Constants.EndOfCentralDirectoryRecord64SignatureBytes))
                    {
                        extension = "zip";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.PKZIP.Constants.DataDescriptorSignatureBytes))
                    {
                        extension = "zip";
                        break;
                    }
                    else if (overlaySample.StartsWith([0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00]))
                    {
                        extension = "rar";
                        break;
                    }
                    else if (overlaySample.StartsWith([0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00]))
                    {
                        extension = "rar";
                        break;
                    }
                    else if (overlaySample.StartsWith(Models.MSDOS.Constants.SignatureBytes))
                    {
                        extension = "bin"; // exe/dll
                        break;
                    }
                }

                // If the extension is unset
                if (extension.Length == 0)
                    return false;

                // Create the temp filename
                string tempFile = $"embedded_overlay.{extension}";
                if (Filename != null)
                    tempFile = $"{Path.GetFileName(Filename)}-{tempFile}";

                tempFile = Path.Combine(outputDirectory, tempFile);
                var directoryName = Path.GetDirectoryName(tempFile);
                if (directoryName != null && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                // Write the resource data to a temp file
                using var tempStream = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                tempStream?.Write(overlayData, overlayOffset, overlayData.Length - overlayOffset);

                return true;
            }
            catch (Exception ex)
            {
                if (includeDebug) Console.Error.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Extract data from a Wise installer
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        public bool ExtractWise(string outputDirectory, bool includeDebug)
        {
            // Get the source data for reading
            Stream source = _dataSource;
            if (Filename != null)
            {
                // Try to open a multipart file
                if (WiseOverlayHeader.OpenFile(Filename, includeDebug, out var temp) && temp != null)
                    source = temp;
            }

            // Try to find the overlay header
            long offset = FindWiseOverlayHeader();
            if (offset > 0 && offset < Length)
                return ExtractWiseOverlay(outputDirectory, includeDebug, source, offset);

            // Everything else could not extract
            return false;
        }

        /// <summary>
        /// Extract using Wise overlay
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="source">Potentially multi-part stream to read</param>
        /// <param name="offset">Offset to the start of the overlay header</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        private bool ExtractWiseOverlay(string outputDirectory, bool includeDebug, Stream source, long offset)
        {
            // Seek to the overlay and parse
            source.Seek(offset, SeekOrigin.Begin);
            var header = WiseOverlayHeader.Create(source);
            if (header == null)
            {
                if (includeDebug) Console.Error.WriteLine("Could not parse the overlay header");
                return false;
            }

            // Extract the header-defined files
            bool extracted = header.ExtractHeaderDefinedFiles(outputDirectory, includeDebug);
            if (!extracted)
            {
                if (includeDebug) Console.Error.WriteLine("Could not extract header-defined files");
                return false;
            }

            // Open the script file from the output directory
            var scriptStream = File.OpenRead(Path.Combine(outputDirectory, "WiseScript.bin"));
            var script = WiseScript.Create(scriptStream);
            if (script == null)
            {
                if (includeDebug) Console.Error.WriteLine("Could not parse WiseScript.bin");
                return false;
            }

            // Get the source directory
            string? sourceDirectory = null;
            if (Filename != null)
                sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(Filename));

            // Process the state machine
            return script.ProcessStateMachine(header, sourceDirectory, outputDirectory, includeDebug);
        }

        #endregion

        #region Resources

        /// <summary>
        /// Find the location of a Wise overlay header, if it exists
        /// </summary>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>Offset to the overlay header on success, -1 otherwise</returns>
        public long FindWiseOverlayHeader()
        {
            // Get the overlay offset
            long overlayOffset = OverlayAddress;
            if (overlayOffset < 0 || overlayOffset >= Length)
                return -1;

            // Attempt to get the overlay header
            _dataSource.Seek(overlayOffset, SeekOrigin.Begin);
            var header = WiseOverlayHeader.Create(_dataSource);
            if (header != null)
                return overlayOffset;

            // Align and loop to see if it can be found
            _dataSource.Seek(overlayOffset, SeekOrigin.Begin);
            _dataSource.AlignToBoundary(0x10);
            overlayOffset = _dataSource.Position;
            while (_dataSource.Position < Length)
            {
                _dataSource.Seek(overlayOffset, SeekOrigin.Begin);
                header = WiseOverlayHeader.Create(_dataSource);
                if (header != null)
                    return overlayOffset;

                overlayOffset += 0x10;
            }

            header = null;
            return -1;
        }

        /// <summary>
        /// Get a single resource entry
        /// </summary>
        /// <param name="id">Resource ID to retrieve</param>
        /// <returns>Resource on success, null otherwise</returns>
        public ResourceTypeResourceEntry? GetResource(int id)
        {
            // If the header is invalid
            if (Header == null)
                return null;

            // Get the available source length, if possible
            long dataLength = Length;
            if (dataLength == -1)
                return null;

            // If the resource table is invalid
            if (ResourceTable?.ResourceTypes == null || ResourceTable.ResourceTypes.Length == 0)
                return null;

            // Loop through the resources to find a matching ID
            foreach (var resourceType in ResourceTable.ResourceTypes)
            {
                // Skip invalid resource types
                if (resourceType.ResourceCount == 0 || resourceType.Resources == null || resourceType.Resources.Length == 0)
                    continue;

                // Loop through the entries to find a matching ID
                foreach (var resource in resourceType.Resources)
                {
                    // Skip non-matching entries
                    if (resource.ResourceID != id)
                        continue;

                    // Return the resource
                    return resource;
                }
            }

            // No entry could be found
            return null;
        }

        /// <summary>
        /// Get the data for a single resource entry
        /// </summary>
        /// <param name="id">Resource ID to retrieve</param>
        /// <returns>Resource data on success, null otherwise</returns>
        public byte[]? GetResourceData(int id)
        {
            // Get the resource offset
            int offset = GetResourceOffset(id);
            if (offset < 0)
                return null;

            // Get the resource length
            int length = GetResourceLength(id);
            if (length < 0)
                return null;
            else if (length == 0)
                return [];

            // Read the resource data and return
            return _dataSource.ReadFrom(offset, length, retainPosition: true);
        }

        /// <summary>
        /// Get the data length for a single resource entry
        /// </summary>
        /// <param name="id">Resource ID to retrieve</param>
        /// <returns>Resource length on success, -1 otherwise</returns>
        public int GetResourceLength(int id)
        {
            // Get the matching resource
            var resource = GetSegment(id);
            if (resource == null)
                return -1;

            // Return the reported length
            return resource.Length;
        }

        /// <summary>
        /// Get the data offset for a single resource entry
        /// </summary>
        /// <param name="id">Resource ID to retrieve</param>
        /// <returns>Resource offset on success, -1 otherwise</returns>
        public int GetResourceOffset(int id)
        {
            // Get the available source length, if possible
            long dataLength = Length;
            if (dataLength == -1)
                return -1;

            // If the resource table is invalid
            if (ResourceTable == null)
                return -1;

            // Get the matching resource
            var resource = GetSegment(id);
            if (resource == null)
                return -1;

            // Verify the resource offset
            int offset = resource.Offset << ResourceTable.AlignmentShiftCount;
            if (offset < 0 || offset + resource.Length >= dataLength)
                return -1;

            // Return the verified offset
            return offset;
        }

        #endregion

        #region Segments

        /// <summary>
        /// Get a single segment
        /// </summary>
        /// <param name="index">Segment index to retrieve</param>
        /// <returns>Segment on success, null otherwise</returns>
        public SegmentTableEntry? GetSegment(int index)
        {
            // If the segment table is invalid
            if (SegmentTable == null || SegmentTable.Length == 0)
                return null;

            // If the index is invalid
            if (index < 0 || index >= SegmentTable.Length)
                return null;

            // Return the segment
            return SegmentTable[index];
        }

        /// <summary>
        /// Get the data for a single segment
        /// </summary>
        /// <param name="index">Segment index to retrieve</param>
        /// <returns>Segment data on success, null otherwise</returns>
        public byte[]? GetSegmentData(int index)
        {
            // Get the segment offset
            int offset = GetSegmentOffset(index);
            if (offset < 0)
                return null;

            // Get the segment length
            int length = GetSegmentLength(index);
            if (length < 0)
                return null;
            else if (length == 0)
                return [];

            // Read the segment data and return
            return _dataSource.ReadFrom(offset, length, retainPosition: true);
        }

        /// <summary>
        /// Get the data length for a single segment
        /// </summary>
        /// <param name="index">Segment index to retrieve</param>
        /// <returns>Segment length on success, -1 otherwise</returns>
        public int GetSegmentLength(int index)
        {
            // Get the matching segment
            var segment = GetSegment(index);
            if (segment == null)
                return -1;

            // Return the reported length
            return segment.Length;
        }

        /// <summary>
        /// Get the data offset for a single segment
        /// </summary>
        /// <param name="index">Segment index to retrieve</param>
        /// <returns>Segment offset on success, -1 otherwise</returns>
        public int GetSegmentOffset(int index)
        {
            // If the header is invalid
            if (Header == null)
                return -1;

            // Get the available source length, if possible
            long dataLength = Length;
            if (dataLength == -1)
                return -1;

            // Get the matching segment
            var segment = GetSegment(index);
            if (segment == null)
                return -1;

            // Verify the segment offset
            int offset = segment.Offset << Header.SegmentAlignmentShiftCount;
            if (offset < 0 || offset + segment.Length >= dataLength)
                return -1;

            // Return the verified offset
            return offset;
        }

        #endregion

        #region REMOVE -- DO NOT USE

        /// <summary>
        /// Read an arbitrary range from the source
        /// </summary>
        /// <param name="rangeStart">The start of where to read data from, -1 means start of source</param>
        /// <param name="length">How many bytes to read, -1 means read until end</param>
        /// <returns>Byte array representing the range, null on error</returns>
        [Obsolete]
        public byte[]? ReadArbitraryRange(int rangeStart = -1, long length = -1)
        {
            // If we have an unset range start, read from the start of the source
            if (rangeStart == -1)
                rangeStart = 0;

            // If we have an unset length, read the whole source
            if (length == -1)
                length = Length;

            return _dataSource.ReadFrom(rangeStart, (int)length, retainPosition: true);
        }

        #endregion
    }
}
