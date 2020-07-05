/*
 *	  NEWEXE.H (C) Copyright Microsoft Corp 1984-1987
 *
 *	  Data structure definitions for the OS/2 & Windows
 *	  executable file format.
 *
 *	  Modified by IVS on 24-Jan-1991 for Resource DeCompiler
 *	  (C) Copyright IVS 1991
 *
 *    http://csn.ul.ie/~caolan/pub/winresdump/winresdump/newexe.h
 */

using System;

namespace WiseUnpacker.Files
{
    internal static class Constants
    {
        public const ushort IMAGE_DOS_SIGNATURE = 0x5A4D;       // MZ
        public const ushort IMAGE_OS2_SIGNATURE = 0x454E;       // NE
        public const ushort IMAGE_OS2_SIGNATURE_LE = 0x454C;    // LE
        public const uint IMAGE_NT_SIGNATURE = 0x00004550;      // PE00

        #region IMAGE_DOS_HEADER

        public const ushort ENEWEXE = 0x40;   // Value of E_LFARLC for new .EXEs
        public const ushort ENEWHDR = 0x003C; // Offset in old hdr. of ptr. to new
        public const ushort ERESWDS = 0x0010; // No. of reserved words (OLD)
        public const ushort ERES1WDS = 0x0004; // No. of reserved words in e_res
        public const ushort ERES2WDS = 0x000A; // No. of reserved words in e_res2
        public const ushort ECP = 0x0004; // Offset in struct of E_CP 
        public const ushort ECBLP = 0x0002; // Offset in struct of E_CBLP
        public const ushort EMINALLOC = 0x000A; // Offset in struct of E_MINALLOC

        #endregion

        #region IMAGE_OS2_HEADER

        public const ushort NERESWORDS = 3;     // 6 bytes reserved
        public const ushort NECRC = 8;          //Offset into new header of NE_CRC

        #endregion

        #region NewSeg

        public const ushort NSALIGN = 9;        // Segment data aligned on 512 byte boundaries
        public const ushort NSLOADED = 0x0004;  // ns_sector field contains memory addr

        #endregion

        #region RsrcNameInfo

        public const ushort RSORDID = 0x8000;       /* if high bit of ID set then integer id */

        /* otherwise ID is offset of string from
           the beginning of the resource table */
        /* Ideally these are the same as the */
        /* corresponding segment flags */
        public const ushort RNMOVE = 0x0010;	  /* Moveable resource */
        public const ushort RNPURE = 0x0020;	  /* Pure (read-only) resource */
        public const ushort RNPRELOAD = 0x0040;	  /* Preloaded resource */
        public const ushort RNDISCARD = 0xF000;	  /* Discard priority level for resource */

        #endregion

        #region IMAGE_OPTIONAL_HEADER

        public const ushort IMAGE_NUMBEROF_DIRECTORY_ENTRIES = 16;

        /* Directory Entries */

        /* Export Directory */
        public const byte IMAGE_DIRECTORY_ENTRY_EXPORT = 0;
        /* Import Directory */
        public const byte IMAGE_DIRECTORY_ENTRY_IMPORT = 1;
        /* Resource Directory */
        public const byte IMAGE_DIRECTORY_ENTRY_RESOURCE = 2;
        /* Exception Directory */
        public const byte IMAGE_DIRECTORY_ENTRY_EXCEPTION = 3;
        /* Security Directory */
        public const byte IMAGE_DIRECTORY_ENTRY_SECURITY = 4;
        /* Base Relocation Table */
        public const byte IMAGE_DIRECTORY_ENTRY_BASERELOC = 5;
        /* Debug Directory */
        public const byte IMAGE_DIRECTORY_ENTRY_DEBUG = 6;
        /* Description String */
        public const byte IMAGE_DIRECTORY_ENTRY_COPYRIGHT = 7;
        /* Machine Value (MIPS GP) */
        public const byte IMAGE_DIRECTORY_ENTRY_GLOBALPTR = 8;
        /* TLS Directory */
        public const byte IMAGE_DIRECTORY_ENTRY_TLS = 9;
        /* Load Configuration Directory */
        public const byte IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG = 10;

        #endregion

        #region IMAGE_SECTION_HEADER

        public const int IMAGE_SIZEOF_SHORT_NAME = 8;

        #endregion

        #region IMAGE_RESOURCE_DATA_ENTRY

        public const uint IMAGE_RESOURCE_DATA_IS_DIRECTORY = 0x80000000;
        public const uint IMAGE_RESOURCE_NAME_IS_STRING	= 0x80000000;

        #endregion
    }

    /// <summary>
    /// DOS 1, 2, 3 .EXE header
    /// </summary>
    internal class IMAGE_DOS_HEADER
    {
        public ushort e_magic;      // 00 Magic number
        public ushort e_cblp;       // 02 Bytes on last page of file
        public ushort e_cp;         // 04 Pages in file
        public ushort e_crlc;       // 06 Relocations
        public ushort e_cparhdr;    // 08 Size of header in paragraphs
        public ushort e_minalloc;   // 0A Minimum extra paragraphs needed
        public ushort e_maxalloc;   // 0C Maximum extra paragraphs needed
        public ushort e_ss;         // 0E Initial (relative) SS value
        public ushort e_sp;         // 10 Initial SP value
        public ushort e_csum;       // 12 Checksum
        public ushort e_ip;         // 14 Initial IP value
        public ushort e_cs;         // 16 Initial (relative) CS value
        public ushort e_lfarlc;     // 18 File address of relocation table
        public ushort e_ovno;       // 1A Overlay number
        public ushort[] e_res;      // 1C Reserved words
        public ushort e_oemid;      // 24 OEM identifier (for e_oeminfo)
        public ushort e_oeminfo;    // 26 OEM information; e_oemid specific
        public ushort[] e_res2;     // 28 Reserved words
        public int e_lfanew;        // 3C File address of new exe header

        public static IMAGE_DOS_HEADER Deserialize(MultiPartFile file)
        {
            IMAGE_DOS_HEADER idh = new IMAGE_DOS_HEADER();

            idh.e_magic = file.ReadUInt16();
            idh.e_cblp = file.ReadUInt16();
            idh.e_cp = file.ReadUInt16();
            idh.e_crlc = file.ReadUInt16();
            idh.e_cparhdr = file.ReadUInt16();
            idh.e_minalloc = file.ReadUInt16();
            idh.e_maxalloc = file.ReadUInt16();
            idh.e_ss = file.ReadUInt16();
            idh.e_sp = file.ReadUInt16();
            idh.e_csum = file.ReadUInt16();
            idh.e_ip = file.ReadUInt16();
            idh.e_cs = file.ReadUInt16();
            idh.e_lfarlc = file.ReadUInt16();
            idh.e_ovno = file.ReadUInt16();
            idh.e_res = new ushort[Constants.ERES1WDS];
            for (int i = 0; i < Constants.ERES1WDS; i++)
            {
                idh.e_res[i] = file.ReadUInt16();
            }
            idh.e_oemid = file.ReadUInt16();
            idh.e_oeminfo = file.ReadUInt16();
            idh.e_res2 = new ushort[Constants.ERES2WDS];
            for (int i = 0; i < Constants.ERES2WDS; i++)
            {
                idh.e_res2[i] = file.ReadUInt16();
            }
            idh.e_lfanew = file.ReadInt32();

            return idh;
        }
    }

    /// <summary>
    /// New .EXE header
    /// </summary>
    internal class IMAGE_OS2_HEADER
    {
        public ushort ne_magic;         // 00 Magic number NE_MAGIC
        public byte ne_ver;             // 02 Linker Version number
        public byte ne_rev;             // 03 Linker Revision number
        public ushort ne_enttab;        // 04 Offset of Entry Table
        public ushort ne_cbenttab;      // 06 Number of bytes in Entry Table
        public uint ne_crc;             // 08 Checksum of whole file
        public ushort ne_flags;         // 0C Flag word
        public ushort ne_autodata;      // 0E Automatic data segment number
        public ushort ne_heap;          // 10 Initial heap allocation
        public ushort ne_stack;         // 12 Initial stack allocation
        public uint ne_csip;            // 14 Initial CS:IP setting
        public uint ne_sssp;            // 18 Initial SS:SP setting
        public ushort ne_cseg;          // 1C Count of file segments
        public ushort ne_cmod;          // 1E Entries in Module Reference Table
        public ushort ne_cbnrestab;     // 20 Size of non-resident name table
        public ushort ne_segtab;        // 22 Offset of Segment Table
        public ushort ne_rsrctab;       // 24 Offset of Resource Table
        public ushort ne_restab;        // 26 Offset of resident name table
        public ushort ne_modtab;        // 28 Offset of Module Reference Table
        public ushort ne_imptab;        // 2A Offset of Imported Names Table
        public uint ne_nrestab;         // 2C Offset of Non-resident Names Table
        public ushort ne_cmovent;       // 30 Count of movable entries
        public ushort ne_align;         // 32 Segment alignment shift count
        public ushort ne_cres;          // 34 Count of resource entries
        public byte ne_exetyp;          // 36 Target operating system
        public byte ne_addflags;        // 37 Additional flags
        public ushort[] ne_res; // 38 3 reserved words
        public byte ne_sdkrev;          // 3E Windows SDK revison number
        public byte ne_sdkver;          // 3F Windows SDK version number

        public static IMAGE_OS2_HEADER Deserialize(MultiPartFile file)
        {
            IMAGE_OS2_HEADER ioh = new IMAGE_OS2_HEADER();

            ioh.ne_magic = file.ReadUInt16();
            ioh.ne_ver = file.ReadByte();
            ioh.ne_rev = file.ReadByte();
            ioh.ne_enttab = file.ReadUInt16();
            ioh.ne_cbenttab = file.ReadUInt16();
            ioh.ne_crc = file.ReadUInt32();
            ioh.ne_flags = file.ReadUInt16();
            ioh.ne_autodata = file.ReadUInt16();
            ioh.ne_heap = file.ReadUInt16();
            ioh.ne_stack = file.ReadUInt16();
            ioh.ne_csip = file.ReadUInt32();
            ioh.ne_sssp = file.ReadUInt32();
            ioh.ne_cseg = file.ReadUInt16();
            ioh.ne_cmod = file.ReadUInt16();
            ioh.ne_cbnrestab = file.ReadUInt16();
            ioh.ne_segtab = file.ReadUInt16();
            ioh.ne_rsrctab = file.ReadUInt16();
            ioh.ne_restab = file.ReadUInt16();
            ioh.ne_modtab = file.ReadUInt16();
            ioh.ne_imptab = file.ReadUInt16();
            ioh.ne_nrestab = file.ReadUInt32();
            ioh.ne_cmovent = file.ReadUInt16();
            ioh.ne_align = file.ReadUInt16();
            ioh.ne_cres = file.ReadUInt16();
            ioh.ne_exetyp = file.ReadByte();
            ioh.ne_addflags = file.ReadByte();
            ioh.ne_res = new ushort[Constants.NERESWORDS];
            for (int i = 0; i < Constants.NERESWORDS; i++)
            {
                ioh.ne_res[i] = file.ReadUInt16();
            }
            ioh.ne_sdkrev = file.ReadByte();
            ioh.ne_sdkver = file.ReadByte();

            return ioh;
        }
    }

    [Flags]
    internal enum TargetOperatingSystems : byte
    {
        NE_UNKNOWN =    0x0,    // Unknown (any "new-format" OS)
        NE_OS2 =        0x1,    // Microsoft/IBM OS/2 (default) 
        NE_WINDOWS =    0x2,    // Microsoft Windows
        NE_DOS4 =       0x3,    // Microsoft MS-DOS 4.x
        NE_WIN386 =     0x4,    // Windows 386  ?????????
    }

    /*
    *  Format of NE_FLAGS(x):
    *
    *  p                   Not-a-process
    *   x                  Unused
    *    e                 Errors in image
    *     x                Unused
    *      b               Bound as family app
    *       ttt            Application type
    *          f           Floating-point instructions
    *           3          386 instructions
    *            2         286 instructions
    *             0        8086 instructions
    *              P       Protected mode only
    *               p      Per-process library initialization
    *                i     Instance data
    *                 s    Solo data
    */
    [Flags]
    internal enum NeFlags : ushort
    {
        NENOTP =            0x8000,   /* Not a process */
        NEIERR =            0x2000,   /* Errors in image */
        NEBOUND =           0x0800,   /* Bound as family app */
        NEAPPTYP =          0x0700,   /* Application type mask */
        NENOTWINCOMPAT =    0x0100,   /* Not compatible with P.M. Windowing */
        NEWINCOMPAT =       0x0200,   /* Compatible with P.M. Windowing */
        NEWINAPI =          0x0300,   /* Uses P.M. Windowing API */
        NEFLTP =            0x0080,   /* Floating-point instructions */
        NEI386 =            0x0040,   /* 386 instructions */
        NEI286 =            0x0020,   /* 286 instructions */
        NEI086 =            0x0010,   /* 8086 instructions */
        NEPROT =            0x0008,   /* Runs in protected mode only */
        NEPPLI =            0x0004,   /* Per-Process Library Initialization */
        NEINST =            0x0002,   /* Instance data */
        NESOLO =            0x0001,   /* Solo data */
    }

    /// <summary>
    /// New .EXE segment table entry
    /// </summary>
    internal class NewSeg
    {
        public ushort ns_sector;    // File sector of start of segment
        public ushort ns_cbseg;     // Number of bytes in file
        public ushort ns_flags;     // Attribute flags
        public ushort ns_minalloc;  // Minimum allocation in bytes

        public static NewSeg Deserialize(MultiPartFile file)
        {
            NewSeg ns = new NewSeg();

            ns.ns_sector = file.ReadUInt16();
            ns.ns_cbseg = file.ReadUInt16();
            ns.ns_flags = file.ReadUInt16();
            ns.ns_minalloc = file.ReadUInt16();

            return ns;
        }
    }

    /*
     *
     *  x                   Unused
     *   h                  Huge segment
     *    c                 32-bit code segment
     *     d                Discardable segment
     *      DD              I/O privilege level (286 DPL bits)
     *        c             Conforming segment
     *         r            Segment has relocations
     *          e           Execute/read only
     *           p          Preload segment
     *            P         Pure segment
     *             m        Movable segment
     *              i       Iterated segment
     *               ttt    Segment type
     */
    internal enum NsFlags : ushort
    {
        NSTYPE = 0x0007,       // Segment type mask
        NSCODE = 0x0000,       // Code segment
        NSDATA = 0x0001,       // Data segment
        NSITER = 0x0008,       // Iterated segment flag
        NSMOVE = 0x0010,       // Movable segment flag
        NSSHARED = 0x0020,     // Shared segment flag
        NSPURE = 0x0020,       // For compatibility
        NSPRELOAD = 0x0040,    // Preload segment flag
        NSEXRD = 0x0080,       // Execute-only (code segment), or read-only (data segment)
        NSRELOC = 0x0100,      // Segment has relocations
        NSCONFORM = 0x0200,    // Conforming segment
        NSDPL = 0x0C00,        // I/O privilege level (286 DPL bits)
        SHIFTDPL = 10,         // Left shift count for SEGDPL field
        NSDISCARD = 0x1000,    // Segment is discardable
        NS32BIT = 0x2000,      // 32-bit code segment
        NSHUGE = 0x4000,       // Huge memory segment, length of segment and minimum allocation size are in segment sector units
    }

    /// <summary>
    /// Segment data
    /// </summary>
    internal class NewSegdata
    {
        // ns_iter
        public ushort ns_niter;    // number of iterations
        public ushort ns_nbytes;   // number of bytes
        public char ns_iterdata;   // iterated data bytes

        // ns_noiter
        public char ns_data;       // data bytes

        public static NewSegdata Deserialize(MultiPartFile file)
        {
            NewSegdata nsd = new NewSegdata();

            // ns_iter
            nsd.ns_niter = file.ReadUInt16();
            nsd.ns_nbytes = file.ReadUInt16();
            nsd.ns_iterdata = file.ReadChar();

            // ns_noiter
            nsd.ns_data = (char)BitConverter.GetBytes(nsd.ns_niter)[0];

            return nsd;
        }
    }

    /// <summary>
    /// Relocation info
    /// </summary>
    internal class NewRlcInfo
    {
        public ushort nr_nreloc;    // number of relocation items that follow
    
        public static NewRlcInfo Deserialize(MultiPartFile file)
        {
            NewRlcInfo nri = new NewRlcInfo();

            nri.nr_nreloc = file.ReadUInt16();

            return nri;
        }
    }

    /// <summary>
    /// Relocation item
    /// </summary>
    internal class NewRlc
    {
        public char nr_stype;      // Source type
        public char nr_flags;      // Flag byte
        public ushort nr_soff;     // Source offset

        // nr_intref - Internal Reference
        public char nr_segno;      // Target segment number
        public char nr_res;        // Reserved
        public ushort nr_entry;    // Target Entry Table offset

        // nr_import - Import
        public ushort nr_mod;      // Index into Module Reference Table
        public ushort nr_proc;     // Procedure ordinal or name offset

        // nr_osfix - Operating system fixup
        public ushort nr_ostype;   // OSFIXUP type
        public ushort nr_osres;    // Reserved

        public static NewRlc Deserialize(MultiPartFile file)
        {
            NewRlc nr = new NewRlc();

            nr.nr_stype = file.ReadChar();
            nr.nr_flags = file.ReadChar();
            nr.nr_soff = file.ReadUInt16();

            // nr_intref
            nr.nr_segno = file.ReadChar();
            nr.nr_res = file.ReadChar();
            nr.nr_entry = file.ReadUInt16();

            // nr_import
            nr.nr_mod = BitConverter.ToUInt16(new byte[] { (byte)nr.nr_stype, (byte)nr.nr_flags }, 0);
            nr.nr_proc = nr.nr_entry;

            // nr_osfix
            nr.nr_ostype = nr.nr_mod;
            nr.nr_osres = nr.nr_proc;

            return nr;
        }
    }

    /*
     *  Format of NR_STYPE(x):
     *
     *  xxxxx       Unused
     *       sss    Source type
     */
    [Flags]
    internal enum NrStype : byte
    {
        NRSTYP =    0x0f,   // Source type mask
        NRSBYT =    0x00,   // lo byte
        NRSSEG =    0x02,   // 16-bit segment
        NRSPTR =    0x03,   // 32-bit pointer
        NRSOFF =    0x05,   // 16-bit offset
        NRSPTR48 =  0x0B,   // 48-bit pointer
        NRSOFF32 =  0x0D,   // 32-bit offset
    }

    /*
     *  Format of NR_FLAGS(x):
     *
     *  xxxxx       Unused
     *       a      Additive fixup
     *        rr    Reference type
     */
    [Flags]
    internal enum NrFlags : byte
    {
        NRADD =     0x04,   // Additive fixup
        NRRTYP =    0x03,   // Reference type mask
        NRRINT =    0x00,   // Internal reference
        NRRORD =    0x01,   // Import by ordinal
        NRRNAM =    0x02,   // Import by name
        NRROSF =    0x03,   // Operating system fixup
    }

    /// <summary>
    /// Resource type or name string
    /// </summary>
    internal class RsrcString
    {
        public char rs_len;         // number of bytes in string
        public char[] rs_string;    // text of string

        public static RsrcString Deserialize(MultiPartFile file)
        {
            RsrcString rs = new RsrcString();

            rs.rs_len = file.ReadChar();
            rs.rs_string = file.ReadChars((byte)rs.rs_len);

            return rs;
        }
    }

    /// <summary>
    /// Resource type information block
    /// </summary>
    internal class RsrcTypeInfo
    {
        public ushort rt_id;
        public ushort rt_nres;
        public uint rt_proc;

        public static RsrcTypeInfo Deserialize(MultiPartFile file)
        {
            RsrcTypeInfo rti = new RsrcTypeInfo();

            rti.rt_id = file.ReadUInt16();
            rti.rt_nres = file.ReadUInt16();
            rti.rt_proc = file.ReadUInt32();

            return rti;
        }
    }

    /// <summary>
    /// Resource name information block
    /// </summary>
    internal class RsrcNameInfo
    {
        /*
         * The following two fields must be shifted left by the value of
         * the rs_align field to compute their actual value. This allows
         * resources to be larger than 64k, but they do not need to be
         * aligned on 512 byte boundaries, the way segments are.
        */
        public ushort rn_offset;    // file offset to resource data
        public ushort rn_length;    // length of resource data
        public ushort rn_flags;     // resource flags
        public ushort rn_id;        // resource name id
        public ushort rn_handle;    // If loaded, then global handle
        public ushort rn_usage;     // Initially zero. Number of times the handle for this resource has been given out

        public static RsrcNameInfo Deserialize(MultiPartFile file)
        {
            RsrcNameInfo rni = new RsrcNameInfo();

            rni.rn_offset = file.ReadUInt16();
            rni.rn_length = file.ReadUInt16();
            rni.rn_flags = file.ReadUInt16();
            rni.rn_id = file.ReadUInt16();
            rni.rn_handle = file.ReadUInt16();
            rni.rn_usage = file.ReadUInt16();

            return rni;
        }
    }

    /// <summary>
    /// Resource table
    /// </summary>
    internal class NewRsrc
    {
        public ushort rs_align;     /* alignment shift count for resources */
        public RsrcTypeInfo rs_typeinfo;

        public static NewRsrc Deserialize(MultiPartFile file)
        {
            NewRsrc nr = new NewRsrc();

            nr.rs_align = file.ReadUInt16();
            nr.rs_typeinfo = RsrcTypeInfo.Deserialize(file);

            return nr;
        }
    }

    /// <summary>
    /// Predefined Resource Types
    /// </summary>
    internal enum ResourceTypes : ushort
    {
        RT_CURSOR = 1,
        RT_BITMAP = 2,
        RT_ICON = 3,
        RT_MENU = 4,
        RT_DIALOG = 5,
        RT_STRING = 6,
        RT_FONTDIR = 7,
        RT_FONT = 8,
        RT_ACCELERATOR = 9,
        RT_RCDATA = 10,
        RT_MESSAGELIST = 11, // RT_MESSAGETABLE
        RT_GROUP_CURSOR = 12,
        RT_RESERVED_1 = 13, // Undefined
        RT_GROUP_ICON = 14,
        RT_RESERVED_2 = 15, // Undefined
        RT_VERSION = 16,
        RT_DLGINCLUDE = 17,
        RT_PLUGPLAY = 19,
        RT_VXD = 20,
        RT_ANICURSOR = 21,

        RT_NEWRESOURCE = 0x2000,
        RT_NEWBITMAP = (RT_BITMAP |RT_NEWRESOURCE),
        RT_NEWMENU = (RT_MENU |RT_NEWRESOURCE),
        RT_NEWDIALOG = (RT_DIALOG |RT_NEWRESOURCE),
        RT_ERROR = 0x7fff,
    }

    internal class NAMEINFO
    {
        public ushort rnOffset;
        public ushort rnLength;
        public ushort rnFlags;
        public ushort rnID;
        public ushort rnHandle;
        public ushort rnUsage;

        public static NAMEINFO Deserialize(MultiPartFile file)
        {
            NAMEINFO ni = new NAMEINFO();

            ni.rnOffset = file.ReadUInt16();
            ni.rnLength = file.ReadUInt16();
            ni.rnFlags = file.ReadUInt16();
            ni.rnID = file.ReadUInt16();
            ni.rnHandle = file.ReadUInt16();
            ni.rnUsage = file.ReadUInt16();

            return ni;
        }
    }

    internal class TYPEINFO
    {
        public ushort rtTypeID;
        public ushort rtResourceCount;
        public uint rtReserved;
        public NAMEINFO rtNameInfo;

        public static TYPEINFO Deserialize(MultiPartFile file)
        {
            TYPEINFO ti = new TYPEINFO();

            ti.rtTypeID = file.ReadUInt16();
            ti.rtResourceCount = file.ReadUInt16();
            ti.rtReserved = file.ReadUInt32();
            ti.rtNameInfo = NAMEINFO.Deserialize(file);

            return ti;
        }
    }

    internal class ResourceTable
    {
        public ushort rscAlignShift;
        public TYPEINFO rscTypes;
        public ushort rscEndTypes;
        public sbyte[][] rscResourceNames;
        public byte rscEndNames;

        public static ResourceTable Deserialize(MultiPartFile file)
        {
            ResourceTable rt = new ResourceTable();

            rt.rscAlignShift = file.ReadUInt16();
            rt.rscTypes = TYPEINFO.Deserialize(file);
            rt.rscEndTypes = file.ReadUInt16();
            rt.rscResourceNames = null; // TODO: Figure out size
            rt.rscEndNames = file.ReadByte();

            return rt;
        }
    }

    internal class IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;

        public static IMAGE_FILE_HEADER Deserialize(MultiPartFile file)
        {
            IMAGE_FILE_HEADER ifh = new IMAGE_FILE_HEADER();

            ifh.Machine = file.ReadUInt16();
            ifh.NumberOfSections = file.ReadUInt16();
            ifh.TimeDateStamp = file.ReadUInt32();
            ifh.PointerToSymbolTable = file.ReadUInt32();
            ifh.NumberOfSymbols = file.ReadUInt32();
            ifh.SizeOfOptionalHeader = file.ReadUInt16();
            ifh.Characteristics = file.ReadUInt16();

            return ifh;
        }
    }

    internal class IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;

        public static IMAGE_DATA_DIRECTORY Deserialize(MultiPartFile file)
        {
            IMAGE_DATA_DIRECTORY idd = new IMAGE_DATA_DIRECTORY();

            idd.VirtualAddress = file.ReadUInt32();
            idd.Size = file.ReadUInt32();

            return idd;
        }
    }

    internal class IMAGE_OPTIONAL_HEADER
    {
        //
        // Standard fields.
        //
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public uint BaseOfData;
        //
        // NT additional fields.
        //
        public uint ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Reserved1;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public uint SizeOfStackReserve;
        public uint SizeOfStackCommit;
        public uint SizeOfHeapReserve;
        public uint SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        public IMAGE_DATA_DIRECTORY[] DataDirectory;

        public static IMAGE_OPTIONAL_HEADER Deserialize(MultiPartFile file)
        {
            IMAGE_OPTIONAL_HEADER ioh = new IMAGE_OPTIONAL_HEADER();

            ioh.Magic = file.ReadUInt16();
            ioh.MajorLinkerVersion = file.ReadByte();
            ioh.MinorLinkerVersion = file.ReadByte();
            ioh.SizeOfCode = file.ReadUInt32();
            ioh.SizeOfInitializedData = file.ReadUInt32();
            ioh.SizeOfUninitializedData = file.ReadUInt32();
            ioh.AddressOfEntryPoint = file.ReadUInt32();
            ioh.BaseOfCode = file.ReadUInt32();
            ioh.BaseOfData = file.ReadUInt32();

            ioh.ImageBase = file.ReadUInt32();
            ioh.SectionAlignment = file.ReadUInt32();
            ioh.FileAlignment = file.ReadUInt32();
            ioh.MajorOperatingSystemVersion = file.ReadUInt16();
            ioh.MinorOperatingSystemVersion = file.ReadUInt16();
            ioh.MajorImageVersion = file.ReadUInt16();
            ioh.MinorImageVersion = file.ReadUInt16();
            ioh.MajorSubsystemVersion = file.ReadUInt16();
            ioh.MinorSubsystemVersion = file.ReadUInt16();
            ioh.Reserved1 = file.ReadUInt32();
            ioh.SizeOfImage = file.ReadUInt32();
            ioh.SizeOfHeaders = file.ReadUInt32();
            ioh.CheckSum = file.ReadUInt32();
            ioh.Subsystem = file.ReadUInt16();
            ioh.DllCharacteristics = file.ReadUInt16();
            ioh.SizeOfStackReserve = file.ReadUInt32();
            ioh.SizeOfStackCommit = file.ReadUInt32();
            ioh.SizeOfHeapReserve = file.ReadUInt32();
            ioh.SizeOfHeapCommit = file.ReadUInt32();
            ioh.LoaderFlags = file.ReadUInt32();
            ioh.NumberOfRvaAndSizes = file.ReadUInt32();
            ioh.DataDirectory = new IMAGE_DATA_DIRECTORY[Constants.IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
            for (int i = 0; i < Constants.IMAGE_NUMBEROF_DIRECTORY_ENTRIES; i++)
            {
                ioh.DataDirectory[i] = IMAGE_DATA_DIRECTORY.Deserialize(file);
            }

            return ioh;
        }
    }

    [Flags]
    internal enum SectionCharacteristics : uint
    {
        CodeSection = 0x00000020,
        InitializedDataSection = 0x00000040,
        UninitializedDataSection = 0x00000080,
        SectionCannotBeCached = 0x04000000,
        SectionIsNotPageable = 0x08000000,
        SectionIsShared = 0x10000000,
        ExecutableSection = 0x20000000,
        ReadableSection = 0x40000000,
        WritableSection = 0x80000000,
    }

    internal class IMAGE_SECTION_HEADER
    {
        public byte[] Name;
        
        // Misc
        public uint PhysicalAddress;
        public uint VirtualSize;

        public uint VirtualAddress;
        public uint SizeOfRawData;
        public uint PointerToRawData;
        public uint PointerToRelocations;
        public uint PointerToLinenumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLinenumbers;
        public SectionCharacteristics Characteristics;

        public static IMAGE_SECTION_HEADER Deserialize(MultiPartFile file)
        {
            IMAGE_SECTION_HEADER ish = new IMAGE_SECTION_HEADER();

            ish.Name = file.ReadBytes(Constants.IMAGE_SIZEOF_SHORT_NAME);

            // Misc
            ish.PhysicalAddress = file.ReadUInt32();
            ish.VirtualSize = ish.PhysicalAddress;

            ish.VirtualAddress = file.ReadUInt32();
            ish.SizeOfRawData = file.ReadUInt32();
            ish.PointerToRawData = file.ReadUInt32();
            ish.PointerToRelocations = file.ReadUInt32();
            ish.PointerToLinenumbers = file.ReadUInt32();
            ish.NumberOfRelocations = file.ReadUInt16();
            ish.NumberOfLinenumbers = file.ReadUInt16();
            ish.Characteristics = (SectionCharacteristics)file.ReadUInt32();

            return ish;
        }
    }

    internal class IMAGE_RESOURCE_DIRECTORY
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ushort NumberOfNamedEntries;
        public ushort NumberOfIdEntries;

        public static IMAGE_RESOURCE_DIRECTORY Deserialize(MultiPartFile file)
        {
            IMAGE_RESOURCE_DIRECTORY ird = new IMAGE_RESOURCE_DIRECTORY();

            ird.Characteristics = file.ReadUInt32();
            ird.TimeDateStamp = file.ReadUInt32();
            ird.MajorVersion = file.ReadUInt16();
            ird.MinorVersion = file.ReadUInt16();
            ird.NumberOfNamedEntries = file.ReadUInt16();
            ird.NumberOfIdEntries = file.ReadUInt16();

            return ird;
        }
    }

    internal class IMAGE_RESOURCE_DIRECTORY_ENTRY
    {
        public uint Name;
        public uint OffsetToData;

        public static IMAGE_RESOURCE_DIRECTORY_ENTRY Deserialize(MultiPartFile file)
        {
            IMAGE_RESOURCE_DIRECTORY_ENTRY irde = new IMAGE_RESOURCE_DIRECTORY_ENTRY();

            irde.Name = file.ReadUInt32();
            irde.OffsetToData = file.ReadUInt32();

            return irde;
        }
    }

    internal class IMAGE_RESOURCE_DATA_ENTRY
    {
        public uint OffsetToData;
        public uint Size;
        public uint CodePage;
        public uint Reserved;

        public static IMAGE_RESOURCE_DATA_ENTRY Deserialize(MultiPartFile file)
        {
            IMAGE_RESOURCE_DATA_ENTRY irde = new IMAGE_RESOURCE_DATA_ENTRY();

            irde.OffsetToData = file.ReadUInt32();
            irde.Size = file.ReadUInt32();
            irde.CodePage = file.ReadUInt32();
            irde.Reserved = file.ReadUInt32();

            return irde;
        }
    }

    internal class IMAGE_RESOURCE_DIR_STRING_U
    {
        public ushort Length;
        public char[] NameString;

        public static IMAGE_RESOURCE_DIR_STRING_U Deserialize(MultiPartFile file)
        {
            IMAGE_RESOURCE_DIR_STRING_U irdsu = new IMAGE_RESOURCE_DIR_STRING_U();

            irdsu.Length = file.ReadUInt16();
            irdsu.NameString = file.ReadChars(irdsu.Length);

            return irdsu;
        }
    }
}
