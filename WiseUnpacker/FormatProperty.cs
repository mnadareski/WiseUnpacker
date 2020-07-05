/// Copyright (c) 1995 by Oliver Fromme  --  All Rights Reserved
/// 
/// Address:  Oliver Fromme, Leibnizstr. 18-61, 38678 Clausthal, Germany
/// Internet:  fromme@rz.tu-clausthal.de
/// WWW:  http://www.tu-clausthal.de/~inof/
/// 
/// Freely distributable, freely usable.
/// The original copyright notice may not be modified or omitted.

using System;
using WiseUnpacker.Files;

namespace WiseUnpacker
{
    internal class FormatProperty : IEquatable<FormatProperty>
    {
        public ExecutableType ExecutableType { get; set; }
        public long ExecutableLength { get; set; }
        public bool Dll { get; set; }
        public long ArchiveStart { get; set; }
        public long ArchiveEnd { get; set; } // Position in the archive head of the archive end
        public bool InitText { get; set; }
        public long FilenamePosition { get; set; }
        public long LCode { get; set; }
        public long LData { get; set; }
        public bool NoCrc { get; set; }

        public bool Equals(FormatProperty other)
        {
            return this.ExecutableLength == other.ExecutableLength
                && this.ExecutableType == other.ExecutableType
                && (this.LCode == other.LCode || other.LCode == -1)
                && (this.LData == other.LData || other.LData == -1);
        }
    }
}
