namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Parse String
    /// 
    /// This action splits a text string and places the results in two variables.
    /// 
    /// You can split the string at a character or substring that you specify, which discards the
    /// character or substring you specified. Example: If you split the string “ONE,TWO” at the
    /// first occurrence of a comma, “ONE” is put into destination variable 1 and “TWO” is put
    /// into the destination variable 2. If the character or substring is not found, the entire
    /// string is put into destination variable 1, and nothing is put into destination variable 2.
    /// The find is case-sensitive.
    /// 
    /// You can also split a string at any arbitrary character position, which discards no
    /// characters. Example: If you split the string “ONE,TWO” at character position four from
    /// left, then “ONE,” is put into the destination variable 1 and “TWO” is put into the
    /// destination variable 2.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f27".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ParseString
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Operation (unknown)
        /// - Trim Spaces (unknown)
        /// - Ignore Case (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Source Value
        /// </summary>
        /// <remarks>
        /// You enter text and variables (examples: %MAINDIR% or %MAINDIR%\%PICTDIR%).
        /// To include a literal percent (%) symbol, use %%.
        /// </remarks>
        public string? Source { get; set; }

        /// <summary>
        /// Pattern/Position at which to split
        /// </summary>
        /// <remarks>
        /// Character patterns are case-sensitive unless you mark Ignore Case.
        /// To split at a pattern, enter any number of characters, including numbers,
        /// and select one of the pattern options in Operation. To split a string
        /// based on character position, enter the character position, where 1 is
        /// the first character, and select one of the position options in Operation.
        /// </remarks>
        public string? PatternPosition { get; set; }

        /// <summary>
        /// Variable to store the first half of the string
        /// </summary>
        public string? DestinationVariable1 { get; set; }

        /// <summary>
        /// Variable to store the second half of the string
        /// </summary>
        public string? DestinationVariable2 { get; set; }
    }
}