namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Wise installer overlay data header
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wiseoverlay.h"/> 
    public class OverlayHeader
    {
        /// <summary>
        /// DLL name length, if it exists
        /// </summary>
        public byte DllNameLen { get; set; }

        /// <summary>
        /// DLL name, missing if <see cref="DllNameLen"/> is 0 
        /// </summary>
        public string? DllName { get; set; }

        /// <summary>
        /// DLL size, missing if <see cref="DllNameLen"/> is 0 
        /// </summary>
        public uint? DllSize { get; set; }

        /// <summary>
        /// Packing flags
        /// </summary>
        public OverlayHeaderFlags Flags { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>20 bytes</remarks>
        public byte[]? Unknown_20 { get; set; }

        /// <summary>
        /// Inflated size of the Wise installer script
        /// </summary>
        /// <remarks>Second file</remarks>
        public uint WiseScriptInflatedSize { get; set; }

        /// <summary>
        /// Deflated size of the Wise installer script
        /// </summary>
        /// <remarks>Second file</remarks>
        public uint WiseScriptDeflatedSize { get; set; }

        /// <summary>
        /// Deflated size of WISE0001.DLL
        /// </summary>
        /// <remarks>Third file</remarks>
        public uint WiseDllDeflatedSize { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public uint UnknownU32_1 { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public uint UnknownU32_2 { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        public uint UnknownU32_3 { get; set; }

        /// <summary>
        /// Deflated size of PROGRESS.DLL
        /// </summary>
        /// <remarks>Fourth file</remarks>
        public uint ProgressDllDeflatedSize { get; set; }

        /// <summary>
        /// Deflated size of unknown data
        /// </summary>
        public uint SomeData6DeflatedSize { get; set; }

        /// <summary>
        /// Deflated size of unknown data
        /// </summary>
        public uint SomeData7DeflatedSize { get; set; }

        /// <summary>
        /// Unknown
        /// </summary>
        /// <remarks>8 bytes</remarks>
        public byte[]? Unknown_8 { get; set; }

        /// <summary>
        /// Deflated size of FILE000{n}.DAT
        /// </summary>
        /// <remarks>Fifth file</remarks>
        public uint SomeData5DeflatedSize { get; set; }

        /// <summary>
        /// Inflated size of FILE000{n}.DAT
        /// </summary>
        /// <remarks>Fifth file</remarks>
        public uint SomeData5InflatedSize { get; set; }

        /// <summary>
        /// On multi-disc installers this is set to 0x00000000, so it may
        /// represent EOF instead of filesize? At least for now. Only compared
        /// the two multi-disc installers listed in the README.md, need more
        /// multi-disc installers to properly compare. On single file
        /// installers this is this installer it's filesize.
        /// </summary>
        public uint EOF { get; set; }

        /// <summary>
        /// Deflated size of the DIB
        /// </summary>
        /// <remarks>First file</remarks>
        public uint DibDeflatedSize { get; set; }

        /// <summary>
        /// Inflated size of the DIB
        /// </summary>
        /// <remarks>First file</remarks>
        public uint DibInflatedSize { get; set; }

        /// <summary>
        /// Endianness of the file(?)
        /// </summary>
        public Endianness Endianness { get; set; }

        /// <summary>
        /// Init text length
        /// </summary>
        public byte InitTextLen { get; set; }

        /// <summary>
        /// Init text whose length is given by <see cref="InitTextLen"/> 
        /// </summary>
        public string? InitText { get; set; }
    }
}
