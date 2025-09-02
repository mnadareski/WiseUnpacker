using SabreTools.Models.NewExecutable;

namespace SabreTools.Serialization
{
    public static partial class Extensions
    {
        /// <summary>
        /// Determine if a resource type information entry is an integer or offset
        /// </summary>
        /// <param name="entry">Resource type information entry to check</param>
        /// <returns>True if the entry is an integer type, false if an offset, null on error</returns>
        public static bool IsIntegerType(this ResourceTypeInformationEntry entry)
            => (entry.TypeID & 0x8000) != 0;

        /// <summary>
        /// Determine if a resource type resource entry is an integer or offset
        /// </summary>
        /// <param name="entry">Resource type resource entry to check</param>
        /// <returns>True if the entry is an integer type, false if an offset, null on error</returns>
        public static bool IsIntegerType(this ResourceTypeResourceEntry entry)
            => (entry.ResourceID & 0x8000) != 0;

        /// <summary>
        /// Get the segment entry type for an entry table bundle
        /// </summary>
        /// <param name="entry">Entry table bundle to check</param>
        /// <returns>SegmentEntryType corresponding to the type</returns>
        public static SegmentEntryType GetEntryType(this EntryTableBundle entry)
        {
            // Determine the entry type based on segment indicator
            if (entry.SegmentIndicator == 0x00)
                return SegmentEntryType.Unused;
            else if (entry.SegmentIndicator >= 0x01 && entry.SegmentIndicator <= 0xFE)
                return SegmentEntryType.FixedSegment;
            else if (entry.SegmentIndicator == 0xFF)
                return SegmentEntryType.MoveableSegment;

            // We should never get here
            return SegmentEntryType.Unused;
        }
    }
}
