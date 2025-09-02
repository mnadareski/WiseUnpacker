using System.Collections.Generic;
using System.IO;
using System.Text;
using SabreTools.IO.Extensions;
using SabreTools.Models.NewExecutable;
using static SabreTools.Models.NewExecutable.Constants;

namespace SabreTools.Serialization.Deserializers
{
    public class NewExecutable : BaseBinaryDeserializer<Executable>
    {
        /// <inheritdoc/>
        public override Executable? Deserialize(Stream? data)
        {
            // If the data is invalid
            if (data == null || !data.CanRead)
                return null;

            try
            {
                // Cache the current offset
                long initialOffset = data.Position;

                // Create a new executable to fill
                var executable = new Executable();

                #region MS-DOS Stub

                // Parse the MS-DOS stub
                var stub = new MSDOS().Deserialize(data);
                if (stub?.Header == null || stub.Header.NewExeHeaderAddr == 0)
                    return null;

                // Set the MS-DOS stub
                executable.Stub = stub;

                #endregion

                #region Executable Header

                // Try to parse the executable header
                data.Seek(initialOffset + stub.Header.NewExeHeaderAddr, SeekOrigin.Begin);
                var header = ParseExecutableHeader(data);
                if (header.Magic != SignatureString)
                    return null;

                // Set the executable header
                executable.Header = header;

                #endregion

                #region Segment Table

                // If the offset for the segment table doesn't exist
                long tableAddress = initialOffset + stub.Header.NewExeHeaderAddr + header.SegmentTableOffset;
                if (tableAddress >= data.Length)
                    return executable;

                // Seek to the segment table
                data.Seek(tableAddress, SeekOrigin.Begin);

                // Set the segment table
                executable.SegmentTable = new SegmentTableEntry[header.FileSegmentCount];
                for (int i = 0; i < header.FileSegmentCount; i++)
                {
                    executable.SegmentTable[i] = ParseSegmentTableEntry(data, initialOffset);
                }

                #endregion

                #region Resource Table

                // If the offset for the segment table doesn't exist
                tableAddress = initialOffset + stub.Header.NewExeHeaderAddr + header.ResourceTableOffset;
                if (tableAddress >= data.Length)
                    return executable;

                // Seek to the resource table
                data.Seek(tableAddress, SeekOrigin.Begin);

                // Set the resource table
                executable.ResourceTable = ParseResourceTable(data, header.ResourceEntriesCount);

                #endregion

                #region Resident-Name Table

                // If the offset for the resident-name table doesn't exist
                tableAddress = initialOffset + stub.Header.NewExeHeaderAddr + header.ResidentNameTableOffset;
                long endOffset = initialOffset + stub.Header.NewExeHeaderAddr + header.ModuleReferenceTableOffset;
                if (tableAddress >= data.Length)
                    return executable;

                // Seek to the resident-name table
                data.Seek(tableAddress, SeekOrigin.Begin);

                // Set the resident-name table
                executable.ResidentNameTable = ParseResidentNameTable(data, endOffset);

                #endregion

                #region Module-Reference Table

                // If the offset for the module-reference table doesn't exist
                tableAddress = initialOffset + stub.Header.NewExeHeaderAddr + header.ModuleReferenceTableOffset;
                if (tableAddress >= data.Length)
                    return executable;

                // Seek to the module-reference table
                data.Seek(tableAddress, SeekOrigin.Begin);

                // Set the module-reference table
                executable.ModuleReferenceTable = new ModuleReferenceTableEntry[header.ModuleReferenceTableSize];
                for (int i = 0; i < header.ModuleReferenceTableSize; i++)
                {
                    executable.ModuleReferenceTable[i] = ParseModuleReferenceTableEntry(data);
                }

                #endregion

                #region Imported-Name Table

                // If the offset for the imported-name table doesn't exist
                tableAddress = initialOffset + stub.Header.NewExeHeaderAddr + header.ImportedNamesTableOffset;
                endOffset = initialOffset + stub.Header.NewExeHeaderAddr + header.EntryTableOffset;
                if (tableAddress >= data.Length)
                    return executable;

                // Seek to the imported-name table
                data.Seek(tableAddress, SeekOrigin.Begin);

                // Set the imported-name table
                executable.ImportedNameTable = ParseImportedNameTable(data, endOffset);

                #endregion

                #region Entry Table

                // If the offset for the imported-name table doesn't exist
                tableAddress = initialOffset + stub.Header.NewExeHeaderAddr + header.EntryTableOffset;
                endOffset = initialOffset + stub.Header.NewExeHeaderAddr + header.EntryTableOffset + header.EntryTableSize;
                if (tableAddress >= data.Length)
                    return executable;

                // Seek to the imported-name table
                data.Seek(tableAddress, SeekOrigin.Begin);

                // Set the entry table
                executable.EntryTable = ParseEntryTable(data, endOffset);

                #endregion

                #region Nonresident-Name Table

                // If the offset for the nonresident-name table doesn't exist
                tableAddress = initialOffset + header.NonResidentNamesTableOffset;
                endOffset = initialOffset + header.NonResidentNamesTableOffset + header.NonResidentNameTableSize;
                if (tableAddress >= data.Length)
                    return executable;

                // Seek to the nonresident-name table
                data.Seek(tableAddress, SeekOrigin.Begin);

                // Set the nonresident-name table
                executable.NonResidentNameTable = ParseNonResidentNameTable(data, endOffset);

                #endregion

                return executable;
            }
            catch
            {
                // Ignore the actual error
                return null;
            }
        }

        /// <summary>
        /// Parse a Stream into an entry table
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="endOffset">First address not part of the entry table</param>
        /// <returns>Filled entry table on success, null on error</returns>
        public static EntryTableBundle[] ParseEntryTable(Stream data, long endOffset)
        {
            var entryTable = new List<EntryTableBundle>();

            while (data.Position < endOffset && data.Position < data.Length)
            {
                var entry = new EntryTableBundle();
                entry.EntryCount = data.ReadByteValue();
                entry.SegmentIndicator = data.ReadByteValue();
                switch (entry.GetEntryType())
                {
                    case SegmentEntryType.Unused:
                        break;

                    case SegmentEntryType.FixedSegment:
                        entry.FixedFlagWord = (FixedSegmentEntryFlag)data.ReadByteValue();
                        entry.FixedOffset = data.ReadUInt16LittleEndian();
                        break;

                    case SegmentEntryType.MoveableSegment:
                        entry.MoveableFlagWord = (MoveableSegmentEntryFlag)data.ReadByteValue();
                        entry.MoveableReserved = data.ReadUInt16LittleEndian();
                        entry.MoveableSegmentNumber = data.ReadByteValue();
                        entry.MoveableOffset = data.ReadUInt16LittleEndian();
                        break;
                }
                entryTable.Add(entry);
            }

            return [.. entryTable];
        }

        /// <summary>
        /// Parse a Stream into an ExecutableHeader
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ExecutableHeader on success, null on error</returns>
        public static ExecutableHeader ParseExecutableHeader(Stream data)
        {
            var obj = new ExecutableHeader();

            byte[] magic = data.ReadBytes(2);
            obj.Magic = Encoding.ASCII.GetString(magic);
            obj.LinkerVersion = data.ReadByteValue();
            obj.LinkerRevision = data.ReadByteValue();
            obj.EntryTableOffset = data.ReadUInt16LittleEndian();
            obj.EntryTableSize = data.ReadUInt16LittleEndian();
            obj.CrcChecksum = data.ReadUInt32LittleEndian();
            obj.FlagWord = (HeaderFlag)data.ReadUInt16LittleEndian();
            obj.AutomaticDataSegmentNumber = data.ReadUInt16LittleEndian();
            obj.InitialHeapAlloc = data.ReadUInt16LittleEndian();
            obj.InitialStackAlloc = data.ReadUInt16LittleEndian();
            obj.InitialCSIPSetting = data.ReadUInt32LittleEndian();
            obj.InitialSSSPSetting = data.ReadUInt32LittleEndian();
            obj.FileSegmentCount = data.ReadUInt16LittleEndian();
            obj.ModuleReferenceTableSize = data.ReadUInt16LittleEndian();
            obj.NonResidentNameTableSize = data.ReadUInt16LittleEndian();
            obj.SegmentTableOffset = data.ReadUInt16LittleEndian();
            obj.ResourceTableOffset = data.ReadUInt16LittleEndian();
            obj.ResidentNameTableOffset = data.ReadUInt16LittleEndian();
            obj.ModuleReferenceTableOffset = data.ReadUInt16LittleEndian();
            obj.ImportedNamesTableOffset = data.ReadUInt16LittleEndian();
            obj.NonResidentNamesTableOffset = data.ReadUInt32LittleEndian();
            obj.MovableEntriesCount = data.ReadUInt16LittleEndian();
            obj.SegmentAlignmentShiftCount = data.ReadUInt16LittleEndian();
            obj.ResourceEntriesCount = data.ReadUInt16LittleEndian();
            obj.TargetOperatingSystem = (Models.NewExecutable.OperatingSystem)data.ReadByteValue();
            obj.AdditionalFlags = (OS2Flag)data.ReadByteValue();
            obj.ReturnThunkOffset = data.ReadUInt16LittleEndian();
            obj.SegmentReferenceThunkOffset = data.ReadUInt16LittleEndian();
            obj.MinCodeSwapAreaSize = data.ReadUInt16LittleEndian();
            obj.WindowsSDKRevision = data.ReadByteValue();
            obj.WindowsSDKVersion = data.ReadByteValue();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an imported-name table
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="endOffset">First address not part of the imported-name table</param>
        /// <returns>Filled imported-name table on success, null on error</returns>
        public static Dictionary<ushort, ImportedNameTableEntry> ParseImportedNameTable(Stream data, long endOffset)
        {
            var obj = new Dictionary<ushort, ImportedNameTableEntry>();

            while (data.Position < endOffset && data.Position < data.Length)
            {
                ushort currentOffset = (ushort)data.Position;
                obj[currentOffset] = ParseImportedNameTableEntry(data);
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an ImportedNameTableEntry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ImportedNameTableEntry on success, null on error</returns>
        public static ImportedNameTableEntry ParseImportedNameTableEntry(Stream data)
        {
            var obj = new ImportedNameTableEntry();

            obj.Length = data.ReadByteValue();
            obj.NameString = data.ReadBytes(obj.Length);

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an ImportOrdinalRelocationRecord
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ImportOrdinalRelocationRecord on success, null on error</returns>
        public static ImportOrdinalRelocationRecord ParseImportOrdinalRelocationRecord(Stream data)
        {
            var obj = new ImportOrdinalRelocationRecord();

            obj.Index = data.ReadUInt16LittleEndian();
            obj.Ordinal = data.ReadUInt16LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an InternalRefRelocationRecord
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled InternalRefRelocationRecord on success, null on error</returns>
        public static InternalRefRelocationRecord ParseInternalRefRelocationRecord(Stream data)
        {
            var obj = new InternalRefRelocationRecord();

            obj.SegmentNumber = data.ReadByteValue();
            obj.Reserved = data.ReadByteValue();
            obj.Offset = data.ReadUInt16LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an ModuleReferenceTableEntry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ModuleReferenceTableEntry on success, null on error</returns>
        public static ModuleReferenceTableEntry ParseModuleReferenceTableEntry(Stream data)
        {
            var obj = new ModuleReferenceTableEntry();

            obj.Offset = data.ReadUInt16LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an ImportNameRelocationRecord
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ImportNameRelocationRecord on success, null on error</returns>
        public static ImportNameRelocationRecord ParseImportNameRelocationRecord(Stream data)
        {
            var obj = new ImportNameRelocationRecord();

            obj.Index = data.ReadUInt16LittleEndian();
            obj.Offset = data.ReadUInt16LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a nonresident-name table
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="endOffset">First address not part of the nonresident-name table</param>
        /// <returns>Filled nonresident-name table on success, null on error</returns>
        public static NonResidentNameTableEntry[] ParseNonResidentNameTable(Stream data, long endOffset)
        {
            var obj = new List<NonResidentNameTableEntry>();

            while (data.Position < endOffset && data.Position < data.Length)
            {
                var entry = ParseNonResidentNameTableEntry(data);
                obj.Add(entry);
            }

            return [.. obj];
        }

        /// <summary>
        /// Parse a Stream into a NonResidentNameTableEntry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled NonResidentNameTableEntry on success, null on error</returns>
        public static NonResidentNameTableEntry ParseNonResidentNameTableEntry(Stream data)
        {
            var obj = new NonResidentNameTableEntry();

            obj.Length = data.ReadByteValue();
            obj.NameString = data.ReadBytes(obj.Length);
            obj.OrdinalNumber = data.ReadUInt16LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an OSFixupRelocationRecord
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled OSFixupRelocationRecord on success, null on error</returns>
        public static OSFixupRelocationRecord ParseOSFixupRelocationRecord(Stream data)
        {
            var obj = new OSFixupRelocationRecord();

            obj.FixupType = (OSFixupType)data.ReadUInt16LittleEndian();
            obj.Reserved = data.ReadUInt16LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an PerSegmentData
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled PerSegmentData on success, null on error</returns>
        public static PerSegmentData ParsePerSegmentData(Stream data)
        {
            var obj = new PerSegmentData();

            obj.RelocationRecordCount = data.ReadUInt16LittleEndian();
            obj.RelocationRecords = new RelocationRecord[obj.RelocationRecordCount];
            for (int i = 0; i < obj.RelocationRecords.Length; i++)
            {
                obj.RelocationRecords[i] = ParseRelocationRecord(data);
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an RelocationRecord
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled RelocationRecord on success, null on error</returns>
        public static RelocationRecord ParseRelocationRecord(Stream data)
        {
            var obj = new RelocationRecord();

            obj.SourceType = (RelocationRecordSourceType)data.ReadByteValue();
            obj.Flags = (RelocationRecordFlag)data.ReadByteValue();
            obj.Offset = data.ReadUInt16LittleEndian();

            switch (obj.Flags & RelocationRecordFlag.TARGET_MASK)
            {
                case RelocationRecordFlag.INTERNALREF:
                    obj.InternalRefRelocationRecord = ParseInternalRefRelocationRecord(data);
                    break;
                case RelocationRecordFlag.IMPORTORDINAL:
                    obj.ImportOrdinalRelocationRecord = ParseImportOrdinalRelocationRecord(data);
                    break;
                case RelocationRecordFlag.IMPORTNAME:
                    obj.ImportNameRelocationRecord = ParseImportNameRelocationRecord(data);
                    break;
                case RelocationRecordFlag.OSFIXUP:
                    obj.OSFixupRelocationRecord = ParseOSFixupRelocationRecord(data);
                    break;
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a resident-name table
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="endOffset">First address not part of the resident-name table</param>
        /// <returns>Filled resident-name table on success, null on error</returns>
        public static ResidentNameTableEntry[] ParseResidentNameTable(Stream data, long endOffset)
        {
            var obj = new List<ResidentNameTableEntry>();

            while (data.Position < endOffset && data.Position < data.Length)
            {
                var entry = ParseResidentNameTableEntry(data);
                obj.Add(entry);
            }

            return [.. obj];
        }

        /// <summary>
        /// Parse a Stream into a ResidentNameTableEntry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ResidentNameTableEntry on success, null on error</returns>
        public static ResidentNameTableEntry ParseResidentNameTableEntry(Stream data)
        {
            var obj = new ResidentNameTableEntry();

            obj.Length = data.ReadByteValue();
            obj.NameString = data.ReadBytes(obj.Length);
            obj.OrdinalNumber = data.ReadUInt16LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ResourceTable
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="count">Number of resource table entries to read</param>
        /// <returns>Filled ResourceTable on success, null on error</returns>
        public static ResourceTable ParseResourceTable(Stream data, ushort count)
        {
            long initialOffset = data.Position;

            var resourceTable = new ResourceTable();

            resourceTable.AlignmentShiftCount = data.ReadUInt16LittleEndian();
            var resourceTypes = new List<ResourceTypeInformationEntry>();

            for (int i = 0; i < count; i++)
            {
                var entry = ParseResourceTypeInformationEntry(data);
                resourceTypes.Add(entry);

                // A zero type ID marks the end of the resource type information blocks.
                if (entry.TypeID == 0)
                    break;
            }

            resourceTable.ResourceTypes = [.. resourceTypes];

            // Get the full list of unique string offsets
            var stringOffsets = new List<ushort>();
            foreach (var rtie in resourceTable.ResourceTypes)
            {
                // Skip invalid entries
                if (rtie == null || rtie.TypeID == 0)
                    continue;

                // Handle offset types
                if (!rtie.IsIntegerType() && !stringOffsets.Contains(rtie.TypeID))
                    stringOffsets.Add(rtie.TypeID);

                // Handle types with resources
                foreach (var rtre in rtie.Resources ?? [])
                {
                    // Skip invalid entries
                    if (rtre == null || rtre.IsIntegerType() || rtre.ResourceID == 0)
                        continue;

                    // Skip already added entries
                    if (stringOffsets.Contains(rtre.ResourceID))
                        continue;

                    stringOffsets.Add(rtre.ResourceID);
                }
            }

            // Order the offsets list
            stringOffsets.Sort();

            // Populate the type and name string dictionary
            resourceTable.TypeAndNameStrings = [];
            for (int i = 0; i < stringOffsets.Count; i++)
            {
                int stringOffset = (int)(stringOffsets[i] + initialOffset);
                data.Seek(stringOffset, SeekOrigin.Begin);

                var str = ParseResourceTypeAndNameString(data);
                resourceTable.TypeAndNameStrings[stringOffsets[i]] = str;
            }

            return resourceTable;
        }

        /// <summary>
        /// Parse a Stream into an ResourceTypeInformationEntry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ResourceTypeInformationEntry on success, null on error</returns>
        public static ResourceTypeInformationEntry ParseResourceTypeInformationEntry(Stream data)
        {
            var obj = new ResourceTypeInformationEntry();

            obj.TypeID = data.ReadUInt16LittleEndian();
            obj.ResourceCount = data.ReadUInt16LittleEndian();
            obj.Reserved = data.ReadUInt32LittleEndian();

            // A zero type ID marks the end of the resource type information blocks.
            if (obj.TypeID == 0)
                return obj;

            obj.Resources = new ResourceTypeResourceEntry[obj.ResourceCount];
            for (int i = 0; i < obj.ResourceCount; i++)
            {
                // TODO: Should we read and store the resource data?
                obj.Resources[i] = ParseResourceTypeResourceEntry(data);
            }

            return obj;
        }

        /// <summary>
        /// Parse a Stream into a ResourceTypeAndNameString
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled rResourceTypeAndNameString on success, null on error</returns>
        public static ResourceTypeAndNameString ParseResourceTypeAndNameString(Stream data)
        {
            var obj = new ResourceTypeAndNameString();

            obj.Length = data.ReadByteValue();
            obj.Text = data.ReadBytes(obj.Length);

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an ResourceTypeResourceEntry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <returns>Filled ResourceTypeResourceEntry on success, null on error</returns>
        public static ResourceTypeResourceEntry ParseResourceTypeResourceEntry(Stream data)
        {
            var obj = new ResourceTypeResourceEntry();

            obj.Offset = data.ReadUInt16LittleEndian();
            obj.Length = data.ReadUInt16LittleEndian();
            obj.FlagWord = (ResourceTypeResourceFlag)data.ReadUInt16LittleEndian();
            obj.ResourceID = data.ReadUInt16LittleEndian();
            obj.Reserved = data.ReadUInt32LittleEndian();

            return obj;
        }

        /// <summary>
        /// Parse a Stream into an SegmentTableEntry
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="initialOffset">Initial offset to use in address comparisons</param>
        /// <returns>Filled SegmentTableEntry on success, null on error</returns>
        public static SegmentTableEntry ParseSegmentTableEntry(Stream data, long initialOffset)
        {
            var obj = new SegmentTableEntry();

            obj.Offset = data.ReadUInt16LittleEndian();
            obj.Length = data.ReadUInt16LittleEndian();
            obj.FlagWord = (SegmentTableEntryFlag)data.ReadUInt16LittleEndian();
            obj.MinimumAllocationSize = data.ReadUInt16LittleEndian();

            // If the data offset is invalid
            if (obj.Offset < 0 || obj.Offset + initialOffset >= data.Length)
                return obj;

            // Cache the current offset
            long currentOffset = data.Position;

            // Seek to the data offset and read
            data.Seek(obj.Offset + initialOffset, SeekOrigin.Begin);
            obj.Data = data.ReadBytes(obj.Length);


#if NET20 || NET35
            if ((obj.FlagWord & SegmentTableEntryFlag.RELOCINFO) != 0)
#else
            if (obj.FlagWord.HasFlag(flag: SegmentTableEntryFlag.RELOCINFO))
#endif
            {
                obj.PerSegmentData = ParsePerSegmentData(data);
            }

            // Seek back to the end of the entry
            data.Seek(currentOffset, SeekOrigin.Begin);

            return obj;
        }
    }
}
