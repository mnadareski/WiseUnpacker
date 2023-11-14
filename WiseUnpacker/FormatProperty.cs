/// Copyright (c) 1995 by Oliver Fromme  --  All Rights Reserved
/// 
/// Address:  Oliver Fromme, Leibnizstr. 18-61, 38678 Clausthal, Germany
/// Internet:  fromme@rz.tu-clausthal.de
/// WWW:  http://www.tu-clausthal.de/~inof/
/// 
/// Freely distributable, freely usable.
/// The original copyright notice may not be modified or omitted.

using System;
using WiseUnpacker.Files.Microsoft;

namespace WiseUnpacker
{
    internal class FormatProperty : IEquatable<FormatProperty>
    {
        public ExecutableType ExecutableType { get; set; }
        public long ExecutableOffset { get; set; }
        public bool Dll { get; set; }
        public long ArchiveStart { get; set; }
        public long ArchiveEnd { get; set; } // Position in the archive head of the archive end
        public bool InitText { get; set; }
        public long FilenamePosition { get; set; }
        public long CodeSectionLength { get; set; }
        public long DataSectionLength { get; set; }
        public bool NoCrc { get; set; }

        public bool Equals(FormatProperty? other)
        {
            if (other == null)
                return false;

            return this.ExecutableOffset == other.ExecutableOffset
                && this.ExecutableType == other.ExecutableType
                && (this.CodeSectionLength == other.CodeSectionLength || other.CodeSectionLength == -1)
                && (this.DataSectionLength == other.DataSectionLength || other.DataSectionLength == -1);
        }

        /// <summary>
        /// Generate an array of all known formats
        /// </summary>
        public static FormatProperty[] GenerateKnownFormats()
        {
            return new FormatProperty[]
            {
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x84b0, Dll = false, ArchiveStart = 0x11, ArchiveEnd = -1,   InitText = false, FilenamePosition = 0x04, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = true  },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3e10, Dll = false, ArchiveStart = 0x1e, ArchiveEnd = -1,   InitText = false, FilenamePosition = 0x04, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3e50, Dll = false, ArchiveStart = 0x1e, ArchiveEnd = -1,   InitText = false, FilenamePosition = 0x04, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3c20, Dll = false, ArchiveStart = 0x1e, ArchiveEnd = -1,   InitText = false, FilenamePosition = 0x04, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3c30, Dll = false, ArchiveStart = 0x22, ArchiveEnd = -1,   InitText = false, FilenamePosition = 0x04, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3660, Dll = false, ArchiveStart = 0x40, ArchiveEnd = 0x3c, InitText = false, FilenamePosition = 0x04, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x36f0, Dll = false, ArchiveStart = 0x48, ArchiveEnd = 0x44, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3770, Dll = false, ArchiveStart = 0x50, ArchiveEnd = 0x4c, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3780, Dll = true,  ArchiveStart = 0x50, ArchiveEnd = 0x4c, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x37b0, Dll = true,  ArchiveStart = 0x50, ArchiveEnd = 0x4c, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x37d0, Dll = true,  ArchiveStart = 0x50, ArchiveEnd = 0x4c, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3c80, Dll = true,  ArchiveStart = 0x5a, ArchiveEnd = 0x4c, InitText = true,  FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3bd0, Dll = true,  ArchiveStart = 0x5a, ArchiveEnd = 0x4c, InitText = true,  FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3c10, Dll = true,  ArchiveStart = 0x5a, ArchiveEnd = 0x4c, InitText = true,  FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },

                new FormatProperty() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x6e00, Dll = false, ArchiveStart = 0x50, ArchiveEnd = 0x4c, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = 0x3cf4, DataSectionLength = 0x1528, NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x6e00, Dll = true,  ArchiveStart = 0x50, ArchiveEnd = 0x4c, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = 0x3cf4, DataSectionLength = 0x1568, NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x6e00, Dll = true,  ArchiveStart = 0x50, ArchiveEnd = 0x4c, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = 0x3d54, DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x6e00, Dll = true,  ArchiveStart = 0x50, ArchiveEnd = 0x4c, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = 0x3d44, DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x6e00, Dll = true,  ArchiveStart = 0x50, ArchiveEnd = 0x4c, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = 0x3d04, DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x3000, Dll = true,  ArchiveStart = 0x50, ArchiveEnd = 0x4c, InitText = false, FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x3800, Dll = true,  ArchiveStart = 0x5a, ArchiveEnd = 0x4c, InitText = true,  FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
                new FormatProperty() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x3a00, Dll = true,  ArchiveStart = 0x5a, ArchiveEnd = 0x4c, InitText = true,  FilenamePosition = 0x1c, CodeSectionLength = -1,     DataSectionLength = -1,     NoCrc = false },
            };
        }
    }
}
