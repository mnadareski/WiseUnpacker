namespace SabreTools.IO.Compression.Deflate
{
    /// <summary>
    /// Represents the status returned from extracting
    /// </summary>
    public enum ExtractionStatus
    {
        /// <summary>
        /// Extraction wasn't performed because the inputs were invalid
        /// </summary>
        INVALID,

        /// <summary>
        /// No issues with the extraction
        /// </summary>
        GOOD,

        /// <summary>
        /// File extracted but was the wrong size
        /// </summary>
        /// <remarks>Rewinds the stream and deletes the bad file</remarks>
        WRONG_SIZE,

        /// <summary>
        /// File extracted but had the wrong CRC-32 value
        /// </summary>
        BAD_CRC,

        /// <summary>
        /// Extraction failed entirely
        /// </summary>
        FAIL,
    }
}