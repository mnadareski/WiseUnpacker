using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SabreTools.IO.Compression.zlib;
using SabreTools.IO.Extensions;
using SabreTools.Matching;
using SabreTools.Models.PortableExecutable.ResourceEntries;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Wrappers
{
    public class PortableExecutable : WrapperBase<Models.PortableExecutable.Executable>, IExtractable
    {
        #region Descriptive Properties

        /// <inheritdoc/>
        public override string DescriptionString => "Portable Executable (PE)";

        #endregion

        #region Extension Properties

        /// <inheritdoc cref="Models.PortableExecutable.Executable.COFFFileHeader"/>
        public Models.PortableExecutable.COFFFileHeader? COFFFileHeader => Model.COFFFileHeader;

        /// <summary>
        /// Dictionary of debug data
        /// </summary>
        public Dictionary<int, object>? DebugData
        {
            get
            {
                lock (_sourceDataLock)
                {
                    // Use the cached data if possible
                    if (_debugData != null && _debugData.Count != 0)
                        return _debugData;

                    // If we have no resource table, just return
                    if (DebugDirectoryTable == null || DebugDirectoryTable.Length == 0)
                        return null;

                    // Otherwise, build and return the cached dictionary
                    ParseDebugTable();
                    return _debugData;
                }
            }
        }

        /// <inheritdoc cref="Models.PortableExecutable.DebugTable.DebugDirectoryTable"/>
        public Models.PortableExecutable.DebugDirectoryEntry[]? DebugDirectoryTable
            => Model.DebugTable?.DebugDirectoryTable;

        /// <summary>
        /// Entry point data, if it exists
        /// </summary>
        public byte[]? EntryPointData
        {
            get
            {
                lock (_sourceDataLock)
                {
                    // If the section table is missing
                    if (SectionTable == null)
                        return null;

                    // If the address is missing
                    if (OptionalHeader?.AddressOfEntryPoint == null)
                        return null;

                    // If we have no entry point
                    int entryPointAddress = (int)OptionalHeader.AddressOfEntryPoint.ConvertVirtualAddress(SectionTable);
                    if (entryPointAddress == 0)
                        return null;

                    // If the entry point matches with the start of a section, use that
                    int entryPointSection = FindEntryPointSectionIndex();
                    if (entryPointSection >= 0 && OptionalHeader.AddressOfEntryPoint == SectionTable[entryPointSection]?.VirtualAddress)
                        return GetSectionData(entryPointSection);

                    // If we already have cached data, just use that immediately
                    if (_entryPointData != null)
                        return _entryPointData;

                    // Read the first 128 bytes of the entry point
                    _entryPointData = _dataSource.ReadFrom(entryPointAddress, length: 128, retainPosition: true);

                    // Cache and return the entry point padding data, even if null
                    return _entryPointData;
                }
            }
        }

        /// <inheritdoc cref="Models.PortableExecutable.Executable.ExportTable"/>
        public Models.PortableExecutable.ExportTable? ExportTable => Model.ExportTable;

        /// <summary>
        /// Header padding data, if it exists
        /// </summary>
        public byte[]? HeaderPaddingData
        {
            get
            {
                lock (_sourceDataLock)
                {
                    // If we already have cached data, just use that immediately
                    if (_headerPaddingData != null)
                        return _headerPaddingData;

                    // TODO: Don't scan the known header data as well

                    // If any required pieces are missing
                    if (Stub?.Header == null)
                        return [];
                    if (SectionTable == null)
                        return [];

                    // Populate the raw header padding data based on the source
                    uint headerStartAddress = Stub.Header.NewExeHeaderAddr;
                    uint firstSectionAddress = uint.MaxValue;
                    foreach (var s in SectionTable)
                    {
                        if (s == null || s.PointerToRawData == 0)
                            continue;
                        if (s.PointerToRawData < headerStartAddress)
                            continue;

                        if (s.PointerToRawData < firstSectionAddress)
                            firstSectionAddress = s.PointerToRawData;
                    }

                    // Check if the header length is more than 0 before reading data
                    int headerLength = (int)(firstSectionAddress - headerStartAddress);
                    if (headerLength <= 0)
                        _headerPaddingData = [];
                    else
                        _headerPaddingData = _dataSource.ReadFrom((int)headerStartAddress, headerLength, retainPosition: true);

                    // Cache and return the header padding data, even if null
                    return _headerPaddingData;
                }
            }
        }

        /// <summary>
        /// Header padding strings, if they exist
        /// </summary>
        public List<string>? HeaderPaddingStrings
        {
            get
            {
                lock (_sourceDataLock)
                {
                    // If we already have cached data, just use that immediately
                    if (_headerPaddingStrings != null)
                        return _headerPaddingStrings;

                    // TODO: Don't scan the known header data as well

                    // If any required pieces are missing
                    if (Stub?.Header == null)
                        return [];
                    if (SectionTable == null)
                        return [];

                    // Populate the header padding strings based on the source
                    uint headerStartAddress = Stub.Header.NewExeHeaderAddr;
                    uint firstSectionAddress = uint.MaxValue;
                    foreach (var s in SectionTable)
                    {
                        if (s == null || s.PointerToRawData == 0)
                            continue;
                        if (s.PointerToRawData < headerStartAddress)
                            continue;

                        if (s.PointerToRawData < firstSectionAddress)
                            firstSectionAddress = s.PointerToRawData;
                    }

                    // Check if the header length is more than 0 before reading strings
                    int headerLength = (int)(firstSectionAddress - headerStartAddress);
                    if (headerLength <= 0)
                        _headerPaddingStrings = [];
                    else
                        _headerPaddingStrings = _dataSource.ReadStringsFrom((int)headerStartAddress, headerLength, charLimit: 3);

                    // Cache and return the header padding data, even if null
                    return _headerPaddingStrings;
                }
            }
        }

        /// <inheritdoc cref="Models.PortableExecutable.Executable.ImportTable"/>
        public Models.PortableExecutable.ImportTable? ImportTable => Model.ImportTable;

        /// <inheritdoc cref="Models.PortableExecutable.Executable.OptionalHeader"/>
        public Models.PortableExecutable.OptionalHeader? OptionalHeader => Model.OptionalHeader;

        /// <summary>
        /// Address of the overlay, if it exists
        /// </summary>
        /// <see href="https://www.autoitscript.com/forum/topic/153277-pe-file-overlay-extraction/"/>
        public int OverlayAddress
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

                    // If the section table is missing
                    if (SectionTable == null)
                        return -1;

                    // If we have certificate data, use that as the end
                    if (OptionalHeader?.CertificateTable != null)
                    {
                        int certificateTableAddress = (int)OptionalHeader.CertificateTable.VirtualAddress.ConvertVirtualAddress(SectionTable);
                        if (certificateTableAddress != 0 && certificateTableAddress < dataLength)
                            dataLength = certificateTableAddress;
                    }

                    // Search through all sections and find the furthest a section goes
                    int endOfSectionData = -1;
                    foreach (var section in SectionTable)
                    {
                        // If we have an invalid section
                        if (section == null)
                            continue;

                        // If we have an invalid section address
                        int sectionAddress = (int)section.VirtualAddress.ConvertVirtualAddress(SectionTable);
                        if (sectionAddress == 0)
                            continue;

                        // If we have an invalid section size
                        if (section.SizeOfRawData == 0 && section.VirtualSize == 0)
                            continue;

                        // Get the real section size
                        int sectionSize = (int)section.SizeOfRawData;

                        // Compare and set the end of section data
                        if (sectionAddress + sectionSize > endOfSectionData)
                            endOfSectionData = sectionAddress + sectionSize;
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
        /// <see href="https://www.autoitscript.com/forum/topic/153277-pe-file-overlay-extraction/"/>
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

                    // If the section table is missing
                    if (SectionTable == null)
                        return null;

                    // If we have certificate data, use that as the end
                    if (OptionalHeader?.CertificateTable != null)
                    {
                        int certificateTableAddress = (int)OptionalHeader.CertificateTable.VirtualAddress.ConvertVirtualAddress(SectionTable);
                        if (certificateTableAddress != 0 && certificateTableAddress < dataLength)
                            dataLength = certificateTableAddress;
                    }

                    // Search through all sections and find the furthest a section goes
                    int endOfSectionData = -1;
                    foreach (var section in SectionTable)
                    {
                        // If we have an invalid section
                        if (section == null)
                            continue;

                        // If we have an invalid section address
                        int sectionAddress = (int)section.VirtualAddress.ConvertVirtualAddress(SectionTable);
                        if (sectionAddress == 0)
                            continue;

                        // If we have an invalid section size
                        if (section.SizeOfRawData == 0 && section.VirtualSize == 0)
                            continue;

                        // Get the real section size
                        int sectionSize = (int)section.SizeOfRawData;

                        // Compare and set the end of section data
                        if (sectionAddress + sectionSize > endOfSectionData)
                            endOfSectionData = sectionAddress + sectionSize;
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
                    _overlayData = _dataSource.ReadFrom(endOfSectionData, (int)overlayLength, retainPosition: true);
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

                    // If the section table is missing
                    if (SectionTable == null)
                        return null;

                    // If we have certificate data, use that as the end
                    if (OptionalHeader?.CertificateTable != null)
                    {
                        int certificateTableAddress = (int)OptionalHeader.CertificateTable.VirtualAddress.ConvertVirtualAddress(SectionTable);
                        if (certificateTableAddress != 0 && certificateTableAddress < dataLength)
                            dataLength = certificateTableAddress;
                    }

                    // Search through all sections and find the furthest a section goes
                    int endOfSectionData = -1;
                    foreach (var section in SectionTable)
                    {
                        // If we have an invalid section
                        if (section == null)
                            continue;

                        // If we have an invalid section address
                        int sectionAddress = (int)section.VirtualAddress.ConvertVirtualAddress(SectionTable);
                        if (sectionAddress == 0)
                            continue;

                        // If we have an invalid section size
                        if (section.SizeOfRawData == 0 && section.VirtualSize == 0)
                            continue;

                        // Get the real section size
                        int sectionSize;
                        if (section.SizeOfRawData < section.VirtualSize)
                            sectionSize = (int)section.VirtualSize;
                        else
                            sectionSize = (int)section.SizeOfRawData;

                        // Compare and set the end of section data
                        if (sectionAddress + sectionSize > endOfSectionData)
                            endOfSectionData = sectionAddress + sectionSize;
                    }

                    // If we didn't find the end of section data
                    if (endOfSectionData <= 0)
                        return null;

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

        /// <inheritdoc cref="Models.PortableExecutable.Executable.ResourceDirectoryTable"/>
        public Models.PortableExecutable.ResourceDirectoryTable? ResourceDirectoryTable => Model.ResourceDirectoryTable;

        /// <summary>
        /// Sanitized section names
        /// </summary>
        public string[]? SectionNames
        {
            get
            {
                lock (_sourceDataLock)
                {
                    // Use the cached data if possible
                    if (_sectionNames != null)
                        return _sectionNames;

                    // If there are no sections
                    if (SectionTable == null)
                        return null;

                    // Otherwise, build and return the cached array
                    _sectionNames = new string[SectionTable.Length];
                    for (int i = 0; i < _sectionNames.Length; i++)
                    {
                        var section = SectionTable[i];
                        if (section == null)
                            continue;

                        // TODO: Handle long section names with leading `/`
                        byte[]? sectionNameBytes = section.Name;
                        if (sectionNameBytes != null)
                        {
                            string sectionNameString = Encoding.UTF8.GetString(sectionNameBytes).TrimEnd('\0');
                            _sectionNames[i] = sectionNameString;
                        }
                    }

                    return _sectionNames;
                }
            }
        }

        /// <inheritdoc cref="Models.PortableExecutable.Executable.SectionTable"/>
        public Models.PortableExecutable.SectionHeader[]? SectionTable => Model.SectionTable;

        /// <inheritdoc cref="Models.PortableExecutable.Executable.Stub"/>
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

        /// <summary>
        /// Dictionary of resource data
        /// </summary>
        public Dictionary<string, object?>? ResourceData
        {
            get
            {
                lock (_sourceDataLock)
                {
                    // Use the cached data if possible
                    if (_resourceData != null && _resourceData.Count != 0)
                        return _resourceData;

                    // If we have no resource table, just return
                    if (OptionalHeader?.ResourceTable == null
                        || OptionalHeader.ResourceTable.VirtualAddress == 0
                        || ResourceDirectoryTable == null)
                        return null;

                    // Otherwise, build and return the cached dictionary
                    ParseResourceDirectoryTable(ResourceDirectoryTable, types: []);
                    return _resourceData;
                }
            }
        }

        #region Version Information

        /// <summary>
        /// "Build GUID"
        /// </summary/>
        public string? BuildGuid => GetVersionInfoString("BuildGuid");

        /// <summary>
        /// "Build signature"
        /// </summary/>
        public string? BuildSignature => GetVersionInfoString("BuildSignature");

        /// <summary>
        /// Additional information that should be displayed for diagnostic purposes.
        /// </summary/>
        public string? Comments => GetVersionInfoString("Comments");

        /// <summary>
        /// Company that produced the file—for example, "Microsoft Corporation" or
        /// "Standard Microsystems Corporation, Inc." This string is required.
        /// </summary/>
        public string? CompanyName => GetVersionInfoString("CompanyName");

        /// <summary>
        /// "Debug version"
        /// </summary/>
        public string? DebugVersion => GetVersionInfoString("DebugVersion");

        /// <summary>
        /// File description to be presented to users. This string may be displayed in a
        /// list box when the user is choosing files to install—for example, "Keyboard
        /// Driver for AT-Style Keyboards". This string is required.
        /// </summary/>
        public string? FileDescription => GetVersionInfoString("FileDescription");

        /// <summary>
        /// Version number of the file—for example, "3.10" or "5.00.RC2". This string
        /// is required.
        /// </summary/>
        public string? FileVersion => GetVersionInfoString("FileVersion");

        /// <summary>
        /// Internal name of the file, if one exists—for example, a module name if the
        /// file is a dynamic-link library. If the file has no internal name, this
        /// string should be the original filename, without extension. This string is required.
        /// </summary/>
        public string? InternalName => GetVersionInfoString(key: "InternalName");

        /// <summary>
        /// Copyright notices that apply to the file. This should include the full text of
        /// all notices, legal symbols, copyright dates, and so on. This string is optional.
        /// </summary/>
        public string? LegalCopyright => GetVersionInfoString(key: "LegalCopyright");

        /// <summary>
        /// Trademarks and registered trademarks that apply to the file. This should include
        /// the full text of all notices, legal symbols, trademark numbers, and so on. This
        /// string is optional.
        /// </summary/>
        public string? LegalTrademarks => GetVersionInfoString(key: "LegalTrademarks");

        /// <summary>
        /// Original name of the file, not including a path. This information enables an
        /// application to determine whether a file has been renamed by a user. The format of
        /// the name depends on the file system for which the file was created. This string
        /// is required.
        /// </summary/>
        public string? OriginalFilename => GetVersionInfoString(key: "OriginalFilename");

        /// <summary>
        /// Information about a private version of the file—for example, "Built by TESTER1 on
        /// \TESTBED". This string should be present only if VS_FF_PRIVATEBUILD is specified in
        /// the fileflags parameter of the root block.
        /// </summary/>
        public string? PrivateBuild => GetVersionInfoString(key: "PrivateBuild");

        /// <summary>
        /// "Product GUID"
        /// </summary/>
        public string? ProductGuid => GetVersionInfoString("ProductGuid");

        /// <summary>
        /// Name of the product with which the file is distributed. This string is required.
        /// </summary/>
        public string? ProductName => GetVersionInfoString(key: "ProductName");

        /// <summary>
        /// Version of the product with which the file is distributed—for example, "3.10" or
        /// "5.00.RC2". This string is required.
        /// </summary/>
        public string? ProductVersion => GetVersionInfoString(key: "ProductVersion");

        /// <summary>
        /// Text that specifies how this version of the file differs from the standard
        /// version—for example, "Private build for TESTER1 solving mouse problems on M250 and
        /// M250E computers". This string should be present only if VS_FF_SPECIALBUILD is
        /// specified in the fileflags parameter of the root block.
        /// </summary/>
        public string? SpecialBuild => GetVersionInfoString(key: "SpecialBuild") ?? GetVersionInfoString(key: "Special Build");

        /// <summary>
        /// "Trade name"
        /// </summary/>
        public string? TradeName => GetVersionInfoString(key: "TradeName");

        /// <summary>
        /// Get the internal version as reported by the resources
        /// </summary>
        /// <returns>Version string, null on error</returns>
        /// <remarks>The internal version is either the file version, product version, or assembly version, in that order</remarks>
        public string? GetInternalVersion()
        {
            string? version = FileVersion;
            if (!string.IsNullOrEmpty(version))
                return version!.Replace(", ", ".");

            version = ProductVersion;
            if (!string.IsNullOrEmpty(version))
                return version!.Replace(", ", ".");

            version = AssemblyVersion;
            if (!string.IsNullOrEmpty(version))
                return version;

            return null;
        }

        #endregion

        #region Manifest Information

        /// <summary>
        /// Description as derived from the assembly manifest
        /// </summary>
        public string? AssemblyDescription
        {
            get
            {
                var manifest = GetAssemblyManifest();
                return manifest?
                    .Description?
                    .Value;
            }
        }

        /// <summary>
        /// Version as derived from the assembly manifest
        /// </summary>
        /// <remarks>
        /// If there are multiple identities included in the manifest,
        /// this will only retrieve the value from the first that doesn't
        /// have a null or empty version.
        /// </remarks>
        public string? AssemblyVersion
        {
            get
            {
                var manifest = GetAssemblyManifest();
                var identities = manifest?.AssemblyIdentities ?? [];
                var versionIdentity = Array.Find(identities, ai => !string.IsNullOrEmpty(ai?.Version));
                return versionIdentity?.Version;
            }
        }

        #endregion

        #endregion

        #region Instance Variables

        /// <summary>
        /// Header padding data, if it exists
        /// </summary>
        private byte[]? _headerPaddingData = null;

        /// <summary>
        /// Header padding strings, if they exist
        /// </summary>
        private List<string>? _headerPaddingStrings = null;

        /// <summary>
        /// Entry point data, if it exists and isn't aligned to a section
        /// </summary>
        private byte[]? _entryPointData = null;

        /// <summary>
        /// Address of the overlay, if it exists
        /// </summary>
        private int? _overlayAddress = null;

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
        /// Sanitized section names
        /// </summary>
        private string[]? _sectionNames = null;

        /// <summary>
        /// Cached raw section data
        /// </summary>
        private byte[][]? _sectionData = null;

        /// <summary>
        /// Cached found string data in sections
        /// </summary>
        private List<string>[]? _sectionStringData = null;

        /// <summary>
        /// Cached raw table data
        /// </summary>
        private byte[][]? _tableData = null;

        /// <summary>
        /// Cached found string data in tables
        /// </summary>
        private List<string>[]? _tableStringData = null;

        /// <summary>
        /// Cached debug data
        /// </summary>
        private readonly Dictionary<int, object> _debugData = [];

        /// <summary>
        /// Cached resource data
        /// </summary>
        private readonly Dictionary<string, object?> _resourceData = [];

        /// <summary>
        /// Cached version info data
        /// </summary>
        private VersionInfo? _versionInfo = null;

        /// <summary>
        /// Cached assembly manifest data
        /// </summary>
        private AssemblyManifest? _assemblyManifest = null;

        /// <summary>
        /// Lock object for reading from the source
        /// </summary>
        private readonly object _sourceDataLock = new();

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public PortableExecutable(Models.PortableExecutable.Executable? model, byte[]? data, int offset)
            : base(model, data, offset)
        {
            // All logic is handled by the base class
        }

        /// <inheritdoc/>
        public PortableExecutable(Models.PortableExecutable.Executable? model, Stream? data)
            : base(model, data)
        {
            // All logic is handled by the base class
        }

        /// <summary>
        /// Create a PE executable from a byte array and offset
        /// </summary>
        /// <param name="data">Byte array representing the executable</param>
        /// <param name="offset">Offset within the array to parse</param>
        /// <returns>A PE executable wrapper on success, null on failure</returns>
        public static PortableExecutable? Create(byte[]? data, int offset)
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
        /// Create a PE executable from a Stream
        /// </summary>
        /// <param name="data">Stream representing the executable</param>
        /// <returns>A PE executable wrapper on success, null on failure</returns>
        public static PortableExecutable? Create(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long currentOffset = data.Position;

                var model = Deserializers.PortableExecutable.DeserializeStream(data);
                if (model == null)
                    return null;

                data.Seek(currentOffset, SeekOrigin.Begin);
                return new PortableExecutable(model, data);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Data

        // TODO: Cache all certificate objects

        /// <summary>
        /// Get the version info string associated with a key, if possible
        /// </summary>
        /// <param name="key">Case-insensitive key to find in the version info</param>
        /// <returns>String representing the data, null on error</returns>
        /// <remarks>
        /// This code does not take into account the locale and will find and return
        /// the first available value. This may not actually matter for version info,
        /// but it is worth mentioning.
        /// </remarks>
        public string? GetVersionInfoString(string key)
        {
            // If we have an invalid key, we can't do anything
            if (string.IsNullOrEmpty(key))
                return null;

            // Ensure that we have the resource data cached
            if (ResourceData == null)
                return null;

            // If we don't have string version info in this executable
            var stringTable = _versionInfo?.StringFileInfo?.Children;
            if (stringTable == null || stringTable.Length == 0)
                return null;

            // Try to find a key that matches
            StringData? match = null;
            foreach (var st in stringTable)
            {
                if (st.Children == null || st.Length == 0)
                    continue;

                // Return the match if found
                match = Array.Find(st.Children, sd => key.Equals(sd.Key, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return match.Value?.TrimEnd('\0');
            }

            return null;
        }

        /// <summary>
        /// Get the assembly manifest, if possible
        /// </summary>
        /// <returns>Assembly manifest object, null on error</returns>
        private AssemblyManifest? GetAssemblyManifest()
        {
            // Use the cached data if possible
            if (_assemblyManifest != null)
                return _assemblyManifest;

            // Ensure that we have the resource data cached
            if (ResourceData == null)
                return null;

            // Return the now-cached assembly manifest
            return _assemblyManifest;
        }

        #endregion

        #region Debug Data

        /// <summary>
        /// Find CodeView debug data by path
        /// </summary>
        /// <param name="path">Partial path to check for</param>
        /// <returns>List of matching debug data</returns>
        public List<object?> FindCodeViewDebugTableByPath(string path)
        {
            // Ensure that we have the debug data cached
            if (DebugData == null)
                return [];

            var debugFound = new List<object?>();
            foreach (var data in DebugData.Values)
            {
                if (data == null)
                    continue;

                if (data is Models.PortableExecutable.NB10ProgramDatabase n)
                {
                    if (n.PdbFileName == null || !n.PdbFileName.Contains(path))
                        continue;

                    debugFound.Add(n);
                }
                else if (data is Models.PortableExecutable.RSDSProgramDatabase r)
                {
                    if (r.PathAndFileName == null || !r.PathAndFileName.Contains(path))
                        continue;

                    debugFound.Add(r);
                }
            }

            return debugFound;
        }

        /// <summary>
        /// Find unparsed debug data by string value
        /// </summary>
        /// <param name="value">String value to check for</param>
        /// <returns>List of matching debug data</returns>
        public List<byte[]?> FindGenericDebugTableByValue(string value)
        {
            // Ensure that we have the resource data cached
            if (DebugData == null)
                return [];

            var table = new List<byte[]?>();
            foreach (var data in DebugData.Values)
            {
                if (data == null)
                    continue;
                if (data is not byte[] b || b == null)
                    continue;

                try
                {
                    string? arrayAsASCII = Encoding.ASCII.GetString(b);
                    if (arrayAsASCII.Contains(value))
                    {
                        table.Add(b);
                        continue;
                    }
                }
                catch { }

                try
                {
                    string? arrayAsUTF8 = Encoding.UTF8.GetString(b);
                    if (arrayAsUTF8.Contains(value))
                    {
                        table.Add(b);
                        continue;
                    }
                }
                catch { }

                try
                {
                    string? arrayAsUnicode = Encoding.Unicode.GetString(b);
                    if (arrayAsUnicode.Contains(value))
                    {
                        table.Add(b);
                        continue;
                    }
                }
                catch { }
            }

            return table;
        }

        #endregion

        #region Debug Parsing

        /// <summary>
        /// Parse the debug directory table information
        /// </summary>
        private void ParseDebugTable()
        {
            // If there is no debug table
            if (DebugDirectoryTable == null || DebugDirectoryTable.Length == 0)
                return;

            // Loop through all debug table entries
            for (int i = 0; i < DebugDirectoryTable.Length; i++)
            {
                var entry = DebugDirectoryTable[i];
                uint address = entry.PointerToRawData;
                uint size = entry.SizeOfData;

                // Read the entry data until we have the end of the stream
                byte[]? entryData;
                try
                {
                    entryData = _dataSource.ReadFrom((int)address, (int)size, retainPosition: true);
                    if (entryData == null || entryData.Length < 4)
                        continue;
                }
                catch (EndOfStreamException)
                {
                    return;
                }

                // If we have CodeView debug data, try to parse it
                if (entry.DebugType == Models.PortableExecutable.DebugType.IMAGE_DEBUG_TYPE_CODEVIEW)
                {
                    // Read the signature
                    int offset = 0;
                    uint signature = entryData.ReadUInt32LittleEndian(ref offset);

                    // Reset the offset
                    offset = 0;

                    // NB10
                    if (signature == 0x3031424E)
                    {
                        var nb10ProgramDatabase = entryData.ParseNB10ProgramDatabase(ref offset);
                        if (nb10ProgramDatabase != null)
                        {
                            _debugData[i] = nb10ProgramDatabase;
                            continue;
                        }
                    }

                    // RSDS
                    else if (signature == 0x53445352)
                    {
                        var rsdsProgramDatabase = entryData.ParseRSDSProgramDatabase(ref offset);
                        if (rsdsProgramDatabase != null)
                        {
                            _debugData[i] = rsdsProgramDatabase;
                            continue;
                        }
                    }
                }
                else
                {
                    _debugData[i] = entryData;
                }
            }
        }

        #endregion

        #region Extraction

        /// <inheritdoc/>
        /// <remarks>
        /// This extracts the following data:
        /// - Archives and executables in the overlay
        /// - Archives and executables in resource data
        /// - CExe-compressed resource data
        /// - SFX archives (7z, MS-CAB, PKZIP, RAR)
        /// - Wise installers
        /// </remarks>
        public bool Extract(string outputDirectory, bool includeDebug)
        {
            bool cexe = ExtractCExe(outputDirectory, includeDebug);
            bool overlay = ExtractFromOverlay(outputDirectory, includeDebug);
            bool resources = ExtractFromResources(outputDirectory, includeDebug);
            bool wise = ExtractWise(outputDirectory, includeDebug);

            return cexe || overlay || resources | wise;
        }

        /// <summary>
        /// Extract a CExe-compressed executable
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        public bool ExtractCExe(string outputDirectory, bool includeDebug)
        {
            try
            {
                // Get all resources of type 99 with index 2
                var resources = FindResourceByNamedType("99, 2");
                if (resources == null || resources.Count == 0)
                    return false;

                // Get the first resource of type 99 with index 2
                var resource = resources[0];
                if (resource == null || resource.Length == 0)
                    return false;

                // Create the output data buffer
                byte[]? data = [];

                // If we had the decompression DLL included, it's zlib
                if (FindResourceByNamedType("99, 1").Count > 0)
                    data = DecompressCExeZlib(resource);
                else
                    data = DecompressCExeLZ(resource);

                // If we have no data
                if (data == null)
                    return false;

                // Create the temp filename
                string tempFile = string.IsNullOrEmpty(Filename) ? "temp.sxe" : $"{Path.GetFileNameWithoutExtension(Filename)}.sxe";
                tempFile = Path.Combine(outputDirectory, tempFile);
                var directoryName = Path.GetDirectoryName(tempFile);
                if (directoryName != null && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                // Write the file data to a temp file
                var tempStream = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                tempStream.Write(data, 0, data.Length);

                return true;
            }
            catch (Exception ex)
            {
                if (includeDebug) Console.Error.WriteLine(ex);
                return false;
            }
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
                for (; overlayOffset < 0x100 && overlayOffset < overlayData.Length - 0x10; overlayOffset++)
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
        /// Extract data from the resources
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        public bool ExtractFromResources(string outputDirectory, bool includeDebug)
        {
            try
            {
                // Cache the resource data for easier reading
                var resourceData = ResourceData;
                if (resourceData == null)
                    return false;

                // Get the resources that have an archive signature
                int i = 0;
                foreach (var value in resourceData.Values)
                {
                    if (value == null || value is not byte[] ba || ba.Length == 0)
                        continue;

                    // Set the output variables
                    int resourceOffset = 0;
                    string extension = string.Empty;

                    // Only process the resource if it a recognized signature
                    for (; resourceOffset < 0x100 && resourceOffset < ba.Length - 0x10; resourceOffset++)
                    {
                        int temp = resourceOffset;
                        byte[] resourceSample = ba.ReadBytes(ref temp, 0x10);

                        if (resourceSample.StartsWith([0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C]))
                        {
                            extension = "7z";
                            break;
                        }
                        else if (resourceSample.StartsWith(Models.MicrosoftCabinet.Constants.SignatureBytes))
                        {
                            extension = "cab";
                            break;
                        }
                        else if (resourceSample.StartsWith(Models.PKZIP.Constants.LocalFileHeaderSignatureBytes))
                        {
                            extension = "zip";
                            break;
                        }
                        else if (resourceSample.StartsWith(Models.PKZIP.Constants.EndOfCentralDirectoryRecordSignatureBytes))
                        {
                            extension = "zip";
                            break;
                        }
                        else if (resourceSample.StartsWith(Models.PKZIP.Constants.EndOfCentralDirectoryRecord64SignatureBytes))
                        {
                            extension = "zip";
                            break;
                        }
                        else if (resourceSample.StartsWith(Models.PKZIP.Constants.DataDescriptorSignatureBytes))
                        {
                            extension = "zip";
                            break;
                        }
                        else if (resourceSample.StartsWith([0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00]))
                        {
                            extension = "rar";
                            break;
                        }
                        else if (resourceSample.StartsWith([0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00]))
                        {
                            extension = "rar";
                            break;
                        }
                        else if (resourceSample.StartsWith(Models.MSDOS.Constants.SignatureBytes))
                        {
                            extension = "bin"; // exe/dll
                            break;
                        }
                    }

                    // If the extension is unset
                    if (extension.Length == 0)
                        continue;

                    try
                    {
                        // Create the temp filename
                        string tempFile = $"embedded_resource_{i++}.{extension}";
                        if (Filename != null)
                            tempFile = $"{Path.GetFileName(Filename)}-{tempFile}";

                        tempFile = Path.Combine(outputDirectory, tempFile);
                        var directoryName = Path.GetDirectoryName(tempFile);
                        if (directoryName != null && !Directory.Exists(directoryName))
                            Directory.CreateDirectory(directoryName);

                        // Write the resource data to a temp file
                        using var tempStream = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                        tempStream?.Write(ba, resourceOffset, ba.Length - resourceOffset);
                    }
                    catch (Exception ex)
                    {
                        if (includeDebug) Console.Error.WriteLine(ex);
                    }
                }

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

            // Try to find the section header
            offset = FindWiseSectionHeader();
            if (offset > 0 && offset < Length)
                return ExtractWiseSection(outputDirectory, includeDebug, source, offset);

            // Everything else could not extract
            return false;
        }

        /// <summary>
        /// Decompress CExe data compressed with LZ
        /// </summary>
        /// <param name="resource">Resource data to inflate</param>
        /// <returns>Inflated data on success, null otherwise</returns>
        private static byte[]? DecompressCExeLZ(byte[] resource)
        {
            try
            {
                var decompressor = IO.Compression.SZDD.Decompressor.CreateSZDD(resource);
                using var dataStream = new MemoryStream();
                decompressor.CopyTo(dataStream);
                return dataStream.ToArray();
            }
            catch
            {
                // Reset the data
                return null;
            }
        }

        /// <summary>
        /// Decompress CExe data compressed with zlib
        /// </summary>
        /// <param name="resource">Resource data to inflate</param>
        /// <returns>Inflated data on success, null otherwise</returns>
        private static byte[]? DecompressCExeZlib(byte[] resource)
        {
            try
            {
                // Inflate the data into the buffer
                var zstream = new ZLib.z_stream_s();
                byte[] data = new byte[resource.Length * 4];
                unsafe
                {
                    fixed (byte* payloadPtr = resource)
                    fixed (byte* dataPtr = data)
                    {
                        zstream.next_in = payloadPtr;
                        zstream.avail_in = (uint)resource.Length;
                        zstream.total_in = (uint)resource.Length;
                        zstream.next_out = dataPtr;
                        zstream.avail_out = (uint)data.Length;
                        zstream.total_out = 0;

                        ZLib.inflateInit_(zstream, ZLib.zlibVersion(), resource.Length);
                        int zret = ZLib.inflate(zstream, 1);
                        ZLib.inflateEnd(zstream);
                    }
                }

                // Trim the buffer to the proper size
                uint read = zstream.total_out;
#if NETFRAMEWORK
                var temp = new byte[read];
                Array.Copy(data, temp, read);
                data = temp;
#else
                data = new ReadOnlySpan<byte>(data, 0, (int)read).ToArray();
#endif
                return data;
            }
            catch
            {
                // Reset the data
                return null;
            }
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

        /// <summary>
        /// Extract using Wise section
        /// </summary>
        /// <param name="outputDirectory">Output directory to write to</param>
        /// <param name="includeDebug">True to include debug data, false otherwise</param>
        /// <param name="source">Potentially multi-part stream to read</param>
        /// <param name="offset">Offset to the start of the section header</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        private bool ExtractWiseSection(string outputDirectory, bool includeDebug, Stream source, long offset)
        {
            // Get the size of the section and seek to the start
            source.Seek(offset, SeekOrigin.Begin);

            // Write section data to new stream
            var header = WiseSectionHeader.Create(source);
            if (header == null)
            {
                if (includeDebug) Console.Error.WriteLine("Could not parse the section header");
                return false;
            }

            // Attempt to extract section
            return header.Extract(outputDirectory, includeDebug);
        }

        #endregion

        #region Resource Data

        /// <summary>
        /// Find dialog box resources by title
        /// </summary>
        /// <param name="title">Dialog box title to check for</param>
        /// <returns>List of matching resources</returns>
        public List<DialogBoxResource?> FindDialogByTitle(string title)
        {
            // Ensure that we have the resource data cached
            if (ResourceData == null)
                return [];

            var resources = new List<DialogBoxResource?>();
            foreach (var resource in ResourceData.Values)
            {
                if (resource == null)
                    continue;
                if (resource is not DialogBoxResource dbr || dbr == null)
                    continue;

                if (dbr.DialogTemplate?.TitleResource?.Contains(title) ?? false)
                    resources.Add(dbr);
                else if (dbr.ExtendedDialogTemplate?.TitleResource?.Contains(title) ?? false)
                    resources.Add(dbr);
            }

            return resources;
        }

        /// <summary>
        /// Find dialog box resources by contained item title
        /// </summary>
        /// <param name="title">Dialog box item title to check for</param>
        /// <returns>List of matching resources</returns>
        public List<DialogBoxResource?> FindDialogBoxByItemTitle(string title)
        {
            // Ensure that we have the resource data cached
            if (ResourceData == null)
                return [];

            var resources = new List<DialogBoxResource?>();
            foreach (var resource in ResourceData.Values)
            {
                if (resource == null)
                    continue;
                if (resource is not DialogBoxResource dbr || dbr == null)
                    continue;

                if (dbr.DialogItemTemplates != null)
                {
                    var templates = Array.FindAll(dbr.DialogItemTemplates, dit => dit?.TitleResource != null);
                    if (Array.FindIndex(templates, dit => dit?.TitleResource?.Contains(title) == true) > -1)
                        resources.Add(dbr);
                }
                else if (dbr.ExtendedDialogItemTemplates != null)
                {
                    var templates = Array.FindAll(dbr.ExtendedDialogItemTemplates, edit => edit?.TitleResource != null);
                    if (Array.FindIndex(templates, edit => edit?.TitleResource?.Contains(title) == true) > -1)
                        resources.Add(dbr);
                }
            }

            return resources;
        }

        /// <summary>
        /// Find string table resources by contained string entry
        /// </summary>
        /// <param name="entry">String entry to check for</param>
        /// <returns>List of matching resources</returns>
        public List<Dictionary<int, string?>?> FindStringTableByEntry(string entry)
        {
            // Ensure that we have the resource data cached
            if (ResourceData == null)
                return [];

            var stringTables = new List<Dictionary<int, string?>?>();
            foreach (var resource in ResourceData.Values)
            {
                if (resource == null)
                    continue;
                if (resource is not Dictionary<int, string?> st || st == null)
                    continue;

                foreach (string? s in st.Values)
                {
#if NETFRAMEWORK || NETSTANDARD
                    if (s == null || !s.Contains(entry))
#else
                    if (s == null || !s.Contains(entry, StringComparison.OrdinalIgnoreCase))
#endif
                        continue;

                    stringTables.Add(st);
                    break;
                }
            }

            return stringTables;
        }

        /// <summary>
        /// Find unparsed resources by type name
        /// </summary>
        /// <param name="typeName">Type name to check for</param>
        /// <returns>List of matching resources</returns>
        public List<byte[]?> FindResourceByNamedType(string typeName)
        {
            // Ensure that we have the resource data cached
            if (ResourceData == null)
                return [];

            var resources = new List<byte[]?>();
            foreach (var kvp in ResourceData)
            {
                if (!kvp.Key.Contains(typeName))
                    continue;
                if (kvp.Value == null || kvp.Value is not byte[] b || b == null)
                    continue;

                resources.Add(b);
            }

            return resources;
        }

        /// <summary>
        /// Find unparsed resources by string value
        /// </summary>
        /// <param name="value">String value to check for</param>
        /// <returns>List of matching resources</returns>
        public List<byte[]?> FindGenericResource(string value)
        {
            // Ensure that we have the resource data cached
            if (ResourceData == null)
                return [];

            var resources = new List<byte[]?>();
            foreach (var resource in ResourceData.Values)
            {
                if (resource == null)
                    continue;
                if (resource is not byte[] b || b == null)
                    continue;

                try
                {
                    string? arrayAsASCII = Encoding.ASCII.GetString(b!);
                    if (arrayAsASCII.Contains(value))
                    {
                        resources.Add(b);
                        continue;
                    }
                }
                catch { }

                try
                {
                    string? arrayAsUTF8 = Encoding.UTF8.GetString(b!);
                    if (arrayAsUTF8.Contains(value))
                    {
                        resources.Add(b);
                        continue;
                    }
                }
                catch { }

                try
                {
                    string? arrayAsUnicode = Encoding.Unicode.GetString(b!);
                    if (arrayAsUnicode.Contains(value))
                    {
                        resources.Add(b);
                        continue;
                    }
                }
                catch { }
            }

            return resources;
        }

        /// <summary>
        /// Find the location of a Wise overlay header, if it exists
        /// </summary>
        /// <returns>Offset to the overlay header on success, -1 otherwise</returns>
        public long FindWiseOverlayHeader()
        {
            // Get the overlay offset
            long overlayOffset = OverlayAddress;

            // Attempt to get the overlay header
            if (overlayOffset >= 0 && overlayOffset < Length)
            {
                _dataSource.Seek(overlayOffset, SeekOrigin.Begin);
                var header = WiseOverlayHeader.Create(_dataSource);
                if (header != null)
                    return overlayOffset;
            }

            // Check section data
            foreach (var section in SectionTable ?? [])
            {
                string sectionName = Encoding.ASCII.GetString(section.Name ?? []).TrimEnd('\0');
                long sectionOffset = section.VirtualAddress.ConvertVirtualAddress(SectionTable);
                _dataSource.Seek(sectionOffset, SeekOrigin.Begin);

                var header = WiseOverlayHeader.Create(_dataSource);
                if (header != null)
                    return sectionOffset;

                // Check after the resource table
                if (sectionName == ".rsrc")
                {
                    // Data immediately following
                    long afterResourceOffset = sectionOffset + section.SizeOfRawData;
                    _dataSource.Seek(afterResourceOffset, SeekOrigin.Begin);

                    header = WiseOverlayHeader.Create(_dataSource);
                    if (header != null)
                        return afterResourceOffset;

                    // Data following padding data
                    _dataSource.Seek(afterResourceOffset, SeekOrigin.Begin);
                    _ = _dataSource.ReadNullTerminatedAnsiString();

                    afterResourceOffset = _dataSource.Position;
                    header = WiseOverlayHeader.Create(_dataSource);
                    if (header != null)
                        return afterResourceOffset;
                }
            }

            // If there are no resources
            if (OptionalHeader?.ResourceTable == null || ResourceData == null)
                return -1;

            // Get the resources that have an executable signature
            bool exeResources = false;
            foreach (var kvp in ResourceData)
            {
                if (kvp.Value == null || kvp.Value is not byte[] ba)
                    continue;
                if (!ba.StartsWith(Models.MSDOS.Constants.SignatureBytes))
                    continue;

                exeResources = true;
                break;
            }

            // If there are no executable resources
            if (!exeResources)
                return -1;

            // Get the raw resource table offset
            long resourceTableOffset = OptionalHeader.ResourceTable.VirtualAddress.ConvertVirtualAddress(SectionTable);
            if (resourceTableOffset <= 0)
                return -1;

            // Search the resource table data for the offset
            long resourceOffset = -1;
            _dataSource.Seek(resourceTableOffset, SeekOrigin.Begin);
            while (_dataSource.Position < resourceTableOffset + OptionalHeader.ResourceTable.Size && _dataSource.Position < _dataSource.Length)
            {
                ushort possibleSignature = _dataSource.ReadUInt16();
                if (possibleSignature == Models.MSDOS.Constants.SignatureUInt16)
                {
                    resourceOffset = _dataSource.Position - 2;
                    break;
                }

                _dataSource.Seek(-1, SeekOrigin.Current);
            }

            // If there was no valid offset, somehow
            if (resourceOffset == -1)
                return -1;

            // Parse the executable and recurse
            _dataSource.Seek(resourceOffset, SeekOrigin.Begin);
            var resourceExe = WrapperFactory.CreateExecutableWrapper(_dataSource);
            if (resourceExe is not PortableExecutable resourcePex)
                return -1;

            return resourcePex.FindWiseOverlayHeader();
        }

        /// <summary>
        /// Find the location of a Wise section header, if it exists
        /// </summary>
        /// <returns>Offset to the section header on success, -1 otherwise</returns>
        public long FindWiseSectionHeader()
        {
            // If the section table is invalid
            if (SectionTable == null)
                return -1;

            // Find the .WISE section
            foreach (var section in SectionTable)
            {
                string sectionName = Encoding.ASCII.GetString(section.Name ?? []).TrimEnd('\0');
                if (sectionName != ".WISE")
                    continue;

                return section.VirtualAddress.ConvertVirtualAddress(SectionTable);
            }

            // Otherwise, it could not be found
            return -1;
        }

        #endregion

        #region Resource Parsing

        /// <summary>
        /// Parse the resource directory table information
        /// </summary>
        private void ParseResourceDirectoryTable(Models.PortableExecutable.ResourceDirectoryTable table, List<object> types)
        {
            if (table?.Entries == null)
                return;

            for (int i = 0; i < table.Entries.Length; i++)
            {
                var entry = table.Entries[i];
                var newTypes = new List<object>(types ?? []);

                if (entry.Name?.UnicodeString != null)
                    newTypes.Add(Encoding.Unicode.GetString(entry.Name.UnicodeString));
                else
                    newTypes.Add(entry.IntegerID);

                ParseResourceDirectoryEntry(entry, newTypes);
            }
        }

        /// <summary>
        /// Parse the name resource directory entry information
        /// </summary>
        private void ParseResourceDirectoryEntry(Models.PortableExecutable.ResourceDirectoryEntry entry, List<object> types)
        {
            if (entry.DataEntry != null)
                ParseResourceDataEntry(entry.DataEntry, types);
            else if (entry.Subdirectory != null)
                ParseResourceDirectoryTable(entry.Subdirectory, types);
        }

        /// <summary>
        /// Parse the resource data entry information
        /// </summary>
        /// <remarks>
        /// When caching the version information and assembly manifest, this code assumes that there is only one of each
        /// of those resources in the entire exectuable. This means that only the last found version or manifest will
        /// ever be cached.
        /// </remarks>
        private void ParseResourceDataEntry(Models.PortableExecutable.ResourceDataEntry entry, List<object> types)
        {
            // Create the key and value objects
            string key = types == null
                ? $"UNKNOWN_{Guid.NewGuid()}"
                : string.Join(", ", Array.ConvertAll([.. types], t => t.ToString()));

            object? value = entry.Data;

            // If we have a known resource type
            if (types != null && types.Count > 0 && types[0] is uint resourceType)
            {
                try
                {
                    switch ((Models.PortableExecutable.ResourceType)resourceType)
                    {
                        case Models.PortableExecutable.ResourceType.RT_CURSOR:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_BITMAP:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_ICON:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_MENU:
                            value = entry.AsMenu();
                            break;
                        case Models.PortableExecutable.ResourceType.RT_DIALOG:
                            value = entry.AsDialogBox();
                            break;
                        case Models.PortableExecutable.ResourceType.RT_STRING:
                            value = entry.AsStringTable();
                            break;
                        case Models.PortableExecutable.ResourceType.RT_FONTDIR:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_FONT:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_ACCELERATOR:
                            value = entry.AsAcceleratorTableResource();
                            break;
                        case Models.PortableExecutable.ResourceType.RT_RCDATA:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_MESSAGETABLE:
                            value = entry.AsMessageResourceData();
                            break;
                        case Models.PortableExecutable.ResourceType.RT_GROUP_CURSOR:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_GROUP_ICON:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_VERSION:
                            _versionInfo = entry.AsVersionInfo();
                            value = _versionInfo;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_DLGINCLUDE:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_PLUGPLAY:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_VXD:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_ANICURSOR:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_ANIICON:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_HTML:
                            value = entry.Data;
                            break;
                        case Models.PortableExecutable.ResourceType.RT_MANIFEST:
                            _assemblyManifest = entry.AsAssemblyManifest();
                            value = _assemblyManifest;
                            break;
                        default:
                            value = entry.Data;
                            break;
                    }
                }
                catch
                {
                    // Fall back on byte array data for malformed items
                    value = entry.Data;
                }
            }

            // If we have a custom resource type
            else if (types != null && types.Count > 0 && types[0] is string)
            {
                value = entry.Data;
            }

            // Add the key and value to the cache
            _resourceData[key] = value;
        }

        #endregion

        #region Sections

        /// <summary>
        /// Determine if a section is contained within the section table
        /// </summary>
        /// <param name="sectionName">Name of the section to check for</param>
        /// <param name="exact">True to enable exact matching of names, false for starts-with</param>
        /// <returns>True if the section is in the executable, false otherwise</returns>
        public bool ContainsSection(string? sectionName, bool exact = false)
        {
            // If no section name is provided
            if (sectionName == null)
                return false;

            // Get all section names first
            if (SectionNames == null)
                return false;

            // If we're checking exactly, return only exact matches
            if (exact)
                return Array.FindIndex(SectionNames, n => n.Equals(sectionName)) > -1;

            // Otherwise, check if section name starts with the value
            else
                return Array.FindIndex(SectionNames, n => n.StartsWith(sectionName)) > -1;
        }

        /// <summary>
        /// Get the section index corresponding to the entry point, if possible
        /// </summary>
        /// <returns>Section index on success, null on error</returns>
        public int FindEntryPointSectionIndex()
        {
            // If the section table is missing
            if (SectionTable == null)
                return -1;

            // If the address is missing
            if (OptionalHeader?.AddressOfEntryPoint == null)
                return -1;

            // If we don't have an entry point
            if (OptionalHeader.AddressOfEntryPoint.ConvertVirtualAddress(SectionTable) == 0)
                return -1;

            // Otherwise, find the section it exists within
            return OptionalHeader.AddressOfEntryPoint.ContainingSectionIndex(SectionTable);
        }

        /// <summary>
        /// Get the first section based on name, if possible
        /// </summary>
        /// <param name="name">Name of the section to check for</param>
        /// <param name="exact">True to enable exact matching of names, false for starts-with</param>
        /// <returns>Section data on success, null on error</returns>
        public Models.PortableExecutable.SectionHeader? GetFirstSection(string? name, bool exact = false)
        {
            // If we have no sections
            if (SectionNames == null || SectionNames.Length == 0 || SectionTable == null || SectionTable.Length == 0)
                return null;

            // If the section doesn't exist
            if (!ContainsSection(name, exact))
                return null;

            // Get the first index of the section
            int index = Array.IndexOf(SectionNames, name);
            if (index == -1)
                return null;

            // Return the section
            return SectionTable[index];
        }

        /// <summary>
        /// Get the last section based on name, if possible
        /// </summary>
        /// <param name="name">Name of the section to check for</param>
        /// <param name="exact">True to enable exact matching of names, false for starts-with</param>
        /// <returns>Section data on success, null on error</returns>
        public Models.PortableExecutable.SectionHeader? GetLastSection(string? name, bool exact = false)
        {
            // If we have no sections
            if (SectionNames == null || SectionNames.Length == 0 || SectionTable == null || SectionTable.Length == 0)
                return null;

            // If the section doesn't exist
            if (!ContainsSection(name, exact))
                return null;

            // Get the last index of the section
            int index = Array.LastIndexOf(SectionNames, name);
            if (index == -1)
                return null;

            // Return the section
            return SectionTable[index];
        }

        /// <summary>
        /// Get the section based on index, if possible
        /// </summary>
        /// <param name="index">Index of the section to check for</param>
        /// <returns>Section data on success, null on error</returns>
        public Models.PortableExecutable.SectionHeader? GetSection(int index)
        {
            // If we have no sections
            if (SectionTable == null || SectionTable.Length == 0)
                return null;

            // If the section doesn't exist
            if (index < 0 || index >= SectionTable.Length)
                return null;

            // Return the section
            return SectionTable[index];
        }

        /// <summary>
        /// Get the first section data based on name, if possible
        /// </summary>
        /// <param name="name">Name of the section to check for</param>
        /// <param name="exact">True to enable exact matching of names, false for starts-with</param>
        /// <returns>Section data on success, null on error</returns>
        public byte[]? GetFirstSectionData(string? name, bool exact = false)
        {
            // If we have no sections
            if (SectionNames == null || SectionNames.Length == 0 || SectionTable == null || SectionTable.Length == 0)
                return null;

            // If the section doesn't exist
            if (!ContainsSection(name, exact))
                return null;

            // Get the first index of the section
            int index = Array.IndexOf(SectionNames, name);
            return GetSectionData(index);
        }

        /// <summary>
        /// Get the last section data based on name, if possible
        /// </summary>
        /// <param name="name">Name of the section to check for</param>
        /// <param name="exact">True to enable exact matching of names, false for starts-with</param>
        /// <returns>Section data on success, null on error</returns>
        public byte[]? GetLastSectionData(string? name, bool exact = false)
        {
            // If we have no sections
            if (SectionNames == null || SectionNames.Length == 0 || SectionTable == null || SectionTable.Length == 0)
                return null;

            // If the section doesn't exist
            if (!ContainsSection(name, exact))
                return null;

            // Get the last index of the section
            int index = Array.LastIndexOf(SectionNames, name);
            return GetSectionData(index);
        }

        /// <summary>
        /// Get the section data based on index, if possible
        /// </summary>
        /// <param name="index">Index of the section to check for</param>
        /// <returns>Section data on success, null on error</returns>
        public byte[]? GetSectionData(int index)
        {
            // If we have no sections
            if (SectionNames == null || SectionNames.Length == 0 || SectionTable == null || SectionTable.Length == 0)
                return null;

            // If the section doesn't exist
            if (index < 0 || index >= SectionTable.Length)
                return null;

            // Get the section data from the table
            var section = SectionTable[index];
            if (section == null)
                return null;

            uint address = section.VirtualAddress.ConvertVirtualAddress(SectionTable);
            if (address == 0)
                return null;

            // Set the section size
            uint size = section.SizeOfRawData;
            lock (_sourceDataLock)
            {
                // Create the section data array if we have to
                _sectionData ??= new byte[SectionNames.Length][];

                // If we already have cached data, just use that immediately
                if (_sectionData[index] != null && _sectionData[index].Length > 0)
                    return _sectionData[index];

                // Populate the raw section data based on the source
                byte[]? sectionData = _dataSource.ReadFrom((int)address, (int)size, retainPosition: true);

                // Cache and return the section data, even if null
                _sectionData[index] = sectionData ?? [];
                return sectionData;
            }
        }

        /// <summary>
        /// Get the first section strings based on name, if possible
        /// </summary>
        /// <param name="name">Name of the section to check for</param>
        /// <param name="exact">True to enable exact matching of names, false for starts-with</param>
        /// <returns>Section strings on success, null on error</returns>
        public List<string>? GetFirstSectionStrings(string? name, bool exact = false)
        {
            // If we have no sections
            if (SectionNames == null || SectionNames.Length == 0 || SectionTable == null || SectionTable.Length == 0)
                return null;

            // If the section doesn't exist
            if (!ContainsSection(name, exact))
                return null;

            // Get the first index of the section
            int index = Array.IndexOf(SectionNames, name);
            return GetSectionStrings(index);
        }

        /// <summary>
        /// Get the last section strings based on name, if possible
        /// </summary>
        /// <param name="name">Name of the section to check for</param>
        /// <param name="exact">True to enable exact matching of names, false for starts-with</param>
        /// <returns>Section strings on success, null on error</returns>
        public List<string>? GetLastSectionStrings(string? name, bool exact = false)
        {
            // If we have no sections
            if (SectionNames == null || SectionNames.Length == 0 || SectionTable == null || SectionTable.Length == 0)
                return null;

            // If the section doesn't exist
            if (!ContainsSection(name, exact))
                return null;

            // Get the last index of the section
            int index = Array.LastIndexOf(SectionNames, name);
            return GetSectionStrings(index);
        }

        /// <summary>
        /// Get the section strings based on index, if possible
        /// </summary>
        /// <param name="index">Index of the section to check for</param>
        /// <returns>Section strings on success, null on error</returns>
        public List<string>? GetSectionStrings(int index)
        {
            // If we have no sections
            if (SectionNames == null || SectionNames.Length == 0 || SectionTable == null || SectionTable.Length == 0)
                return null;

            // If the section doesn't exist
            if (index < 0 || index >= SectionTable.Length)
                return null;

            // Get the section data from the table
            var section = SectionTable[index];
            if (section == null)
                return null;

            uint address = section.VirtualAddress.ConvertVirtualAddress(SectionTable);
            if (address == 0)
                return null;

            // Set the section size
            uint size = section.SizeOfRawData;
            lock (_sourceDataLock)
            {
                // Create the section string array if we have to
                _sectionStringData ??= new List<string>[SectionNames.Length];

                // If we already have cached data, just use that immediately
                if (_sectionStringData[index] != null && _sectionStringData[index].Count > 0)
                    return _sectionStringData[index];

                // Populate the section string data based on the source
                List<string>? sectionStringData = _dataSource.ReadStringsFrom((int)address, (int)size);

                // Cache and return the section string data, even if null
                _sectionStringData[index] = sectionStringData ?? [];
                return sectionStringData;
            }
        }

        #endregion

        #region Tables

        /// <summary>
        /// Get the table based on index, if possible
        /// </summary>
        /// <param name="index">Index of the table to check for</param>
        /// <returns>Table on success, null on error</returns>
        public Models.PortableExecutable.DataDirectory? GetTable(int index)
        {
            // If the table doesn't exist
            if (OptionalHeader == null || index < 0 || index > 16)
                return null;

            return index switch
            {
                1 => OptionalHeader.ExportTable,
                2 => OptionalHeader.ImportTable,
                3 => OptionalHeader.ResourceTable,
                4 => OptionalHeader.ExceptionTable,
                5 => OptionalHeader.CertificateTable,
                6 => OptionalHeader.BaseRelocationTable,
                7 => OptionalHeader.Debug,
                8 => null, // Architecture Table
                9 => OptionalHeader.GlobalPtr,
                10 => OptionalHeader.ThreadLocalStorageTable,
                11 => OptionalHeader.LoadConfigTable,
                12 => OptionalHeader.BoundImport,
                13 => OptionalHeader.ImportAddressTable,
                14 => OptionalHeader.DelayImportDescriptor,
                15 => OptionalHeader.CLRRuntimeHeader,
                16 => null, // Reserved

                // Should never be possible
                _ => null,
            };
        }

        /// <summary>
        /// Get the table data based on index, if possible
        /// </summary>
        /// <param name="index">Index of the table to check for</param>
        /// <returns>Table data on success, null on error</returns>
        public byte[]? GetTableData(int index)
        {
            // If the table doesn't exist
            if (OptionalHeader == null || index < 0 || index > 16)
                return null;

            // Get the table from the optional header
            var table = GetTable(index);

            // Get the virtual address and size from the entries
            uint virtualAddress = table?.VirtualAddress ?? 0;
            uint size = table?.Size ?? 0;

            // If there is  no section table
            if (SectionTable == null)
                return null;

            // Get the physical address from the virtual one
            uint address = virtualAddress.ConvertVirtualAddress(SectionTable);
            if (address == 0 || size == 0)
                return null;

            lock (_sourceDataLock)
            {
                // Create the table data array if we have to
                _tableData ??= new byte[16][];

                // If we already have cached data, just use that immediately
                if (_tableData[index] != null && _tableData[index].Length > 0)
                    return _tableData[index];

                // Populate the raw table data based on the source
                byte[]? tableData = _dataSource.ReadFrom((int)address, (int)size, retainPosition: true);

                // Cache and return the table data, even if null
                _tableData[index] = tableData ?? [];
                return tableData;
            }
        }

        /// <summary>
        /// Get the table strings based on index, if possible
        /// </summary>
        /// <param name="index">Index of the table to check for</param>
        /// <returns>Table strings on success, null on error</returns>
        public List<string>? GetTableStrings(int index)
        {
            // If the table doesn't exist
            if (OptionalHeader == null || index < 0 || index > 16)
                return null;

            // Get the table from the optional header
            var table = GetTable(index);

            // Get the virtual address and size from the entries
            uint virtualAddress = table?.VirtualAddress ?? 0;
            uint size = table?.Size ?? 0;

            // If there is  no section table
            if (SectionTable == null)
                return null;

            // Get the physical address from the virtual one
            uint address = virtualAddress.ConvertVirtualAddress(SectionTable);
            if (address == 0 || size == 0)
                return null;

            lock (_sourceDataLock)
            {
                // Create the table string array if we have to
                _tableStringData ??= new List<string>[16];

                // If we already have cached data, just use that immediately
                if (_tableStringData[index] != null && _tableStringData[index].Count > 0)
                    return _tableStringData[index];

                // Populate the table string data based on the source
                List<string>? tableStringData = _dataSource.ReadStringsFrom((int)address, (int)size);

                // Cache and return the table string data, even if null
                _tableStringData[index] = tableStringData ?? [];
                return tableStringData;
            }
        }

        #endregion
    }
}
