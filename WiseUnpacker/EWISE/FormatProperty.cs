/// Copyright (c) 1995 by Oliver Fromme  --  All Rights Reserved
/// 
/// Address:  Oliver Fromme, Leibnizstr. 18-61, 38678 Clausthal, Germany
/// Internet:  fromme@rz.tu-clausthal.de
/// WWW:  http://www.tu-clausthal.de/~inof/
/// 
/// Freely distributable, freely usable.
/// The original copyright notice may not be modified or omitted.

using System;

namespace WiseUnpacker.EWISE
{
    public class FormatProperty : IEquatable<FormatProperty>
    {
        public ExecutableType ExecutableType { get; set; }
        public long ExecutableOffset { get; set; }
        public long ArchiveEnd { get; set; } // Position in the archive head of the archive end
        public long CodeSectionLength { get; set; }
        public long DataSectionLength { get; set; }

        public bool Equals(FormatProperty? other)
        {
            if (other == null)
                return false;

            return ExecutableOffset == other.ExecutableOffset
                && ExecutableType == other.ExecutableType
                && (CodeSectionLength == other.CodeSectionLength || other.CodeSectionLength == -1)
                && (DataSectionLength == other.DataSectionLength || other.DataSectionLength == -1);
        }

        /// <summary>
        /// Array of all known formats
        /// </summary>
        public static FormatProperty[] KnownFormats
        {
            get =>
            [
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3660, ArchiveEnd = 0x3c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x36f0, ArchiveEnd = 0x44, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3770, ArchiveEnd = 0x4c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3780, ArchiveEnd = 0x4c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x37b0, ArchiveEnd = 0x4c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x37d0, ArchiveEnd = 0x4c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3bd0, ArchiveEnd = 0x4c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3c10, ArchiveEnd = 0x4c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3c20, ArchiveEnd = -1,   CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3c30, ArchiveEnd = -1,   CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3c80, ArchiveEnd = 0x4c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3e10, ArchiveEnd = -1,   CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x3e50, ArchiveEnd = -1,   CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.NE, ExecutableOffset = 0x84b0, ArchiveEnd = -1,   CodeSectionLength = -1,     DataSectionLength = -1 },

                new() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x3000, ArchiveEnd = 0x4c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x3800, ArchiveEnd = 0x4c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x3a00, ArchiveEnd = 0x4c, CodeSectionLength = -1,     DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x6e00, ArchiveEnd = 0x4c, CodeSectionLength = 0x3cf4, DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x6e00, ArchiveEnd = 0x4c, CodeSectionLength = 0x3d04, DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x6e00, ArchiveEnd = 0x4c, CodeSectionLength = 0x3d44, DataSectionLength = -1 },
                new() { ExecutableType = ExecutableType.PE, ExecutableOffset = 0x6e00, ArchiveEnd = 0x4c, CodeSectionLength = 0x3d54, DataSectionLength = -1 },
            ];
        }
    }
}
