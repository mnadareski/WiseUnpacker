using System.IO;
using System.Text;
using SabreTools.IO.Extensions;

namespace WiseUnpacker
{
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wiseoverlay.h"/> 
    public enum WiseOverlayHeaderFlags : uint
    {
        UNKNOWN_1 = 1 << 0,

        UNKNOWN_2 = 1 << 1,

        UNKNOWN_3 = 1 << 2,

        UNKNOWN_4 = 1 << 3,

        /// <remarks>
        /// Seen in hluplink.exe, Swat 3 and glsetup.exe
        /// </remarks>
        UNKNOWN_5 = 1 << 4,

        UNKNOWN_6 = 1 << 5,

        UNKNOWN_7 = 1 << 6,

        UNKNOWN_8 = 1 << 7,

        PK_ZIP = 1 << 8,

        UNKNOWN_10 = 1 << 9,

        UNKNOWN_11 = 1 << 10,

        UNKNOWN_12 = 1 << 11,

        UNKNOWN_13 = 1 << 12,

        UNKNOWN_14 = 1 << 13,

        UNKNOWN_15 = 1 << 14,

        UNKNOWN_16 = 1 << 15,

        UNKNOWN_17 = 1 << 16,

        /// <remarks>
        /// Only seen in Swat 3
        /// </remarks>
        UNKNOWN_18 = 1 << 17,

        UNKNOWN_19 = 1 << 18,

        /// <remarks>
        /// Only seen set in Wild Wheels
        /// </remarks>
        UNKNOWN_20 = 1 << 19,

        UNKNOWN_21 = 1 << 20,

        UNKNOWN_22 = 1 << 21,

        /// <remarks>
        /// Only seen in glsetup.exe
        /// </remarks>
        UNKNOWN_23 = 1 << 22,

        UNKNOWN_24 = 1 << 23,

        UNKNOWN_25 = 1 << 24,

        UNKNOWN_26 = 1 << 25,

        UNKNOWN_27 = 1 << 26,

        UNKNOWN_28 = 1 << 27,

        UNKNOWN_29 = 1 << 28,

        UNKNOWN_30 = 1 << 29,

        UNKNOWN_31 = 1 << 30,

        UNKNOWN_32 = (uint)1 << 31,
    }

    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wiseoverlay.h"/> 
    public class WiseOverlayHeader
    {
        public byte DllNameLength { get; }

        public string? DllName { get; }

        /// <remarks>
        /// Only has a value if <see cref="DllNameLength"/> is greater than 0 
        /// </remarks>
        public uint? DllSize { get; }

        public WiseOverlayHeaderFlags Flags { get; }

        /// <summary>
        /// 20 bytes of unknown data
        /// </summary>
        public byte[] Unknown0 { get; } = new byte[20];

        public uint WiseScriptUncompressedSize { get; }

        public uint WiseScriptCompressedSize { get; }

        public uint WiseDllCompressedSize { get; }

        public uint Unknown1 { get; }

        public uint Unknown2 { get; }

        public uint Unknown3 { get; }

        public uint ProgressDllCompressedSize { get; }
        
        public uint Unknown6CompressedSize { get; }

        public uint Unknown7CompressedSize { get; }

        /// <summary>
        /// 8 bytes of unknown data
        /// </summary>
        public byte[] Unknown8 { get; } = new byte[8];

        public uint FileDatCompressedSize { get; }

        public uint FileDatUncompressedSize { get; }

        /// <summary>
        /// On multi-disc installers this is set to 0x00000000, so it may
        /// represent EOF instead of filesize? At least for now. Only compared
        /// the two multi-disc installers listed in the README.md, need more
        /// multi-disc installers to properly compare. On single file
        /// installers this is this installer it's filesize.
        /// </summary>
        public uint Eof { get; }

        public uint DibCompressedSize { get; }

        public uint DibUncompressedSize { get; }

        /// <summary>
        /// Always 0x08000 (LE) / 0x0008 (BE, as in file?)
        /// </summary>
        public byte[] Endianness { get; } = new byte[2];

        public byte InitTextLength { get; }

        public string? InitText { get; }

        public WiseOverlayHeader(Stream data)
        {
            DllNameLength = data.ReadByteValue();
            if (DllNameLength > 0)
            {
                byte[] dllName = data.ReadBytes(DllNameLength);
                DllName = Encoding.ASCII.GetString(dllName);
                DllSize = data.ReadUInt32();
            }

            Flags = (WiseOverlayHeaderFlags)data.ReadUInt32();
            Unknown0 = data.ReadBytes(20);
            WiseScriptUncompressedSize = data.ReadUInt32();
            WiseScriptCompressedSize = data.ReadUInt32();
            WiseDllCompressedSize = data.ReadUInt32();
            Unknown1 = data.ReadUInt32();
            Unknown2 = data.ReadUInt32();
            Unknown3 = data.ReadUInt32();
            ProgressDllCompressedSize = data.ReadUInt32();
            Unknown6CompressedSize = data.ReadUInt32();
            Unknown7CompressedSize = data.ReadUInt32();
            Unknown8 = data.ReadBytes(8);
            FileDatCompressedSize = data.ReadUInt32();
            FileDatUncompressedSize = data.ReadUInt32();
            Eof = data.ReadUInt32();
            DibCompressedSize = data.ReadUInt32();
            DibUncompressedSize = data.ReadUInt32();
            Endianness = data.ReadBytes(2);
            InitTextLength = data.ReadByteValue();
            if (InitTextLength > 0)
            {
                byte[] initText = data.ReadBytes(InitTextLength);
                InitText = Encoding.ASCII.GetString(initText);
            }
        }
    }
}