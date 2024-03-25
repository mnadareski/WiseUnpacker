using System.Text;
using SabreTools.IO;
using WiseUnpacker.Files;
using NE = SabreTools.Models.NewExecutable;
using PE = SabreTools.Models.PortableExecutable;

namespace WiseUnpacker
{
    // TODO: Remove once the relevant methods are surfaced from the serialization library
    internal class Serializer
    {
        /// <summary>
        /// Create a COFF file header from a multipart file
        /// </summary>
        public static PE.COFFFileHeader CreateCOFFFileHeader(MultiPartFile file)
        {
            var ifh = new PE.COFFFileHeader();

            ifh.Machine = (PE.MachineType)file.ReadUInt16();
            ifh.NumberOfSections = file.ReadUInt16();
            ifh.TimeDateStamp = file.ReadUInt32();
            ifh.PointerToSymbolTable = file.ReadUInt32();
            ifh.NumberOfSymbols = file.ReadUInt32();
            ifh.SizeOfOptionalHeader = file.ReadUInt16();
            ifh.Characteristics = (PE.Characteristics)file.ReadUInt16();

            return ifh;
        }

        /// <summary>
        /// Create a data directory from a multipart file
        /// </summary>
        public static PE.DataDirectory CreateDataDirectory(MultiPartFile file)
        {
            var idd = new PE.DataDirectory();

            idd.VirtualAddress = file.ReadUInt32();
            idd.Size = file.ReadUInt32();

            return idd;
        }

        /// <summary>
        /// Create an information entry from a multipart file
        /// </summary>
        public static NE.ResourceTypeInformationEntry CreateInformationEntry(MultiPartFile file)
        {
            var rti = new NE.ResourceTypeInformationEntry();

            rti.TypeID = file.ReadUInt16();
            rti.ResourceCount = file.ReadUInt16();
            rti.Reserved = file.ReadUInt32();
            rti.Resources = new NE.ResourceTypeResourceEntry[rti.ResourceCount];

            return rti;
        }

        /// <summary>
        /// Create an MS-DOS executable header from a multipart file
        /// </summary>
        public static SabreTools.Models.MSDOS.ExecutableHeader CreateMSDOSExecutableHeader(MultiPartFile file)
        {
            var idh = new SabreTools.Models.MSDOS.ExecutableHeader();

            var magicBytes = file.ReadBytes(2);
            idh.Magic = Encoding.ASCII.GetString(magicBytes!);
            idh.LastPageBytes = file.ReadUInt16();
            idh.Pages = file.ReadUInt16();
            idh.RelocationItems = file.ReadUInt16();
            idh.HeaderParagraphSize = file.ReadUInt16();
            idh.MinimumExtraParagraphs = file.ReadUInt16();
            idh.MaximumExtraParagraphs = file.ReadUInt16();
            idh.InitialSSValue = file.ReadUInt16();
            idh.InitialSPValue = file.ReadUInt16();
            idh.Checksum = file.ReadUInt16();
            idh.InitialIPValue = file.ReadUInt16();
            idh.InitialCSValue = file.ReadUInt16();
            idh.RelocationTableAddr = file.ReadUInt16();
            idh.OverlayNumber = file.ReadUInt16();
            idh.Reserved1 = new ushort[4];
            for (int i = 0; i < idh.Reserved1.Length; i++)
            {
                idh.Reserved1[i] = file.ReadUInt16();
            }
            idh.OEMIdentifier = file.ReadUInt16();
            idh.OEMInformation = file.ReadUInt16();
            idh.Reserved2 = new ushort[10];
            for (int i = 0; i < idh.Reserved2.Length; i++)
            {
                idh.Reserved2[i] = file.ReadUInt16();
            }
            idh.NewExeHeaderAddr = file.ReadUInt32();

            return idh;
        }

        /// <summary>
        /// Create a New Executable header from a multipart file
        /// </summary>
        public static NE.ExecutableHeader CreateNEExecutableHeader(MultiPartFile file)
        {
            var ioh = new NE.ExecutableHeader();

            var magicBytes = file.ReadBytes(2);
            ioh.Magic = Encoding.ASCII.GetString(magicBytes!);
            ioh.LinkerVersion = file.ReadByteValue();
            ioh.LinkerRevision = file.ReadByteValue();
            ioh.EntryTableOffset = file.ReadUInt16();
            ioh.EntryTableSize = file.ReadUInt16();
            ioh.CrcChecksum = file.ReadUInt32();
            ioh.FlagWord = (NE.HeaderFlag)file.ReadUInt16();
            ioh.AutomaticDataSegmentNumber = file.ReadUInt16();
            ioh.InitialHeapAlloc = file.ReadUInt16();
            ioh.InitialStackAlloc = file.ReadUInt16();
            ioh.InitialCSIPSetting = file.ReadUInt32();
            ioh.InitialSSSPSetting = file.ReadUInt32();
            ioh.FileSegmentCount = file.ReadUInt16();
            ioh.ModuleReferenceTableSize = file.ReadUInt16();
            ioh.NonResidentNameTableSize = file.ReadUInt16();
            ioh.SegmentTableOffset = file.ReadUInt16();
            ioh.ResourceTableOffset = file.ReadUInt16();
            ioh.ResidentNameTableOffset = file.ReadUInt16();
            ioh.ModuleReferenceTableOffset = file.ReadUInt16();
            ioh.ImportedNamesTableOffset = file.ReadUInt16();
            ioh.NonResidentNamesTableOffset = file.ReadUInt32();
            ioh.MovableEntriesCount = file.ReadUInt16();
            ioh.SegmentAlignmentShiftCount = file.ReadUInt16();
            ioh.ResourceEntriesCount = file.ReadUInt16();
            ioh.TargetOperatingSystem = (NE.OperatingSystem)file.ReadByteValue();
            ioh.AdditionalFlags = (NE.OS2Flag)file.ReadByteValue();
            ioh.ReturnThunkOffset = file.ReadUInt16();
            ioh.SegmentReferenceThunkOffset = file.ReadUInt16();
            ioh.MinCodeSwapAreaSize = file.ReadUInt16();
            ioh.WindowsSDKRevision = file.ReadByteValue();
            ioh.WindowsSDKVersion = file.ReadByteValue();

            return ioh;
        }

        /// <summary>
        /// Create an optional header from a multipart file
        /// </summary>
        public static PE.OptionalHeader CreateOptionalHeader(MultiPartFile file)
        {
            var ioh = new PE.OptionalHeader();

            ioh.Magic = (PE.OptionalHeaderMagicNumber)file.ReadUInt16();
            ioh.MajorLinkerVersion = file.ReadByteValue();
            ioh.MinorLinkerVersion = file.ReadByteValue();
            ioh.SizeOfCode = file.ReadUInt32();
            ioh.SizeOfInitializedData = file.ReadUInt32();
            ioh.SizeOfUninitializedData = file.ReadUInt32();
            ioh.AddressOfEntryPoint = file.ReadUInt32();
            ioh.BaseOfCode = file.ReadUInt32();
            ioh.BaseOfData = file.ReadUInt32();

            ioh.ImageBase_PE32 = file.ReadUInt32();
            ioh.SectionAlignment = file.ReadUInt32();
            ioh.FileAlignment = file.ReadUInt32();
            ioh.MajorOperatingSystemVersion = file.ReadUInt16();
            ioh.MinorOperatingSystemVersion = file.ReadUInt16();
            ioh.MajorImageVersion = file.ReadUInt16();
            ioh.MinorImageVersion = file.ReadUInt16();
            ioh.MajorSubsystemVersion = file.ReadUInt16();
            ioh.MinorSubsystemVersion = file.ReadUInt16();
            ioh.Reserved = file.ReadUInt32();
            ioh.SizeOfImage = file.ReadUInt32();
            ioh.SizeOfHeaders = file.ReadUInt32();
            ioh.CheckSum = file.ReadUInt32();
            ioh.Subsystem = (PE.WindowsSubsystem)file.ReadUInt16();
            ioh.DllCharacteristics = (PE.DllCharacteristics)file.ReadUInt16();
            ioh.SizeOfStackReserve_PE32 = file.ReadUInt32();
            ioh.SizeOfStackCommit_PE32 = file.ReadUInt32();
            ioh.SizeOfHeapReserve_PE32 = file.ReadUInt32();
            ioh.SizeOfHeapCommit_PE32 = file.ReadUInt32();
            ioh.LoaderFlags = file.ReadUInt32();
            ioh.NumberOfRvaAndSizes = file.ReadUInt32();

            ioh.ExportTable = CreateDataDirectory(file);
            ioh.ImportTable = CreateDataDirectory(file);
            ioh.ResourceTable = CreateDataDirectory(file);
            ioh.ExceptionTable = CreateDataDirectory(file);
            ioh.CertificateTable = CreateDataDirectory(file);
            ioh.BaseRelocationTable = CreateDataDirectory(file);
            ioh.Debug = CreateDataDirectory(file);
            ioh.Architecture = file.ReadUInt64();
            ioh.GlobalPtr = CreateDataDirectory(file);
            ioh.ThreadLocalStorageTable = CreateDataDirectory(file);
            ioh.LoadConfigTable = CreateDataDirectory(file);
            ioh.BoundImport = CreateDataDirectory(file);
            ioh.ImportAddressTable = CreateDataDirectory(file);
            ioh.DelayImportDescriptor = CreateDataDirectory(file);
            ioh.CLRRuntimeHeader = CreateDataDirectory(file);
            ioh.Reserved = file.ReadUInt64();

            return ioh;
        }

        /// <summary>
        /// Create a resource entry from a multipart file
        /// </summary>
        public static NE.ResourceTypeResourceEntry CreateResourceEntry(MultiPartFile file)
        {
            var rni = new NE.ResourceTypeResourceEntry();

            rni.Offset = file.ReadUInt16();
            rni.Length = file.ReadUInt16();
            rni.FlagWord = (NE.ResourceTypeResourceFlag)file.ReadUInt16();
            rni.ResourceID = file.ReadUInt16();
            rni.Reserved = file.ReadUInt32();

            return rni;
        }

        /// <summary>
        /// Create a section header from a multipart file
        /// </summary>
        public static PE.SectionHeader CreateSectionHeader(MultiPartFile file)
        {
            var ish = new PE.SectionHeader();

            ish.Name = file.ReadBytes(8);

            ish.VirtualSize = file.ReadUInt32();
            ish.VirtualAddress = file.ReadUInt32();
            ish.SizeOfRawData = file.ReadUInt32();
            ish.PointerToRawData = file.ReadUInt32();
            ish.PointerToRelocations = file.ReadUInt32();
            ish.PointerToLinenumbers = file.ReadUInt32();
            ish.NumberOfRelocations = file.ReadUInt16();
            ish.NumberOfLinenumbers = file.ReadUInt16();
            ish.Characteristics = (PE.SectionFlags)file.ReadUInt32();

            return ish;
        }

        /// <summary>
        /// Create a segment table entry from a multipart file
        /// </summary>
        public static NE.SegmentTableEntry CreateSegmentTableEntry(MultiPartFile file)
        {
            var ns = new NE.SegmentTableEntry();

            ns.Offset = file.ReadUInt16();
            ns.Length = file.ReadUInt16();
            ns.FlagWord = (NE.SegmentTableEntryFlag)file.ReadUInt16();
            ns.MinimumAllocationSize = file.ReadUInt16();

            return ns;
        }
    }
}
