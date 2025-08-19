using System;
using System.Collections.Generic;

namespace SabreTools.Models.WiseInstaller
{
    public static class Constants
    {
        /// <summary>
        /// Count of per-language strings for an Action
        /// </summary>
        /// <remarks>Derived from WISE0001.DLL</remarks>
        public static readonly byte[] CountOfLanguageActionStrings =
        [
            0x01, 0x00, 0x04, 0x02, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        ];

        /// <summary>
        /// Count of non-language strings for an Action
        /// </summary>
        /// <remarks>
        /// Derived from WISE0001.DLL
        /// One variant of the DLL has action 0x06 have a
        /// value of `0x01` instead of `0x00`.
        /// One variant of the DLL has action 0x18 have a
        /// value of `0x01` instead of `0x00`.
        /// </remarks>
        public static readonly byte[] CountOfStaticActionStrings =
        [
            0x02, 0x00, 0x01, 0x00, 0x00, 0x03, 0x00, 0x03,
            0x00, 0x04, 0x03, 0x01, 0x02, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x03, 0x00, 0x02, 0x02, 0x01, 0x01,
            0x00, 0x03, 0x02, 0x00, 0x01, 0x02, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00,
        ];

        /// <summary>
        /// Size of the invariant data for an Action
        /// </summary>
        /// <remarks>
        /// One variant of the DLL has action 0x18 have a
        /// value of `0x06` instead of `0x01`.
        /// </remarks>
        public static readonly byte[] SizeOfStaticActionData =
        [
            0x2B, 0x00, 0x02, 0x02, 0x02, 0x01, 0x13, 0x02,
            0x02, 0x02, 0x03, 0x02, 0x02, 0x01, 0x00, 0x01,
            0x01, 0x01, 0x2B, 0x00, 0x0D, 0x02, 0x01, 0x06,
            0x01, 0x02, 0x02, 0x01, 0x01, 0x01, 0x02, 0x00,
            0x00, 0x00, 0x00, 0x02, 0x01, 0x01, 0x00, 0x00,
        ];
        
        /// <summary>
        /// "WIS" string for WiseSection 57, 49, 53
        /// </summary>
        public static readonly byte[] WisString =
        [0x57, 0x49, 0x53];
        
        /// <summary>
        /// List of currently observed offsets for the "WIS" string in WiseSection
        /// </summary>
        public static readonly int[] WisOffsets =
        [32, 33, 41, 77, 78, 82];
        
        /// <summary>
        /// Size of the header for a WiseSection
        /// </summary>
        public static readonly Dictionary<int, int> WiseSectionHeaderLengthDictionary = new Dictionary<int, int>()
        {
            {32, 6},
            {33, 6},
            {41, 8},
            {77, 17},
            {78, 17},
            {82, 18},
        };
        
        /// <summary>
        /// Offset from "WIS" string to be used as length of version field.
        /// </summary>
        public static readonly Dictionary<int, int> WiseSectionVersionOffsetDictionary = new Dictionary<int, int>()
        {
            {32, 4},
            {33, 5},
            {41, 5},
            {77, 5},
            {78, 6},
            {82, 6},
        };
    }
}