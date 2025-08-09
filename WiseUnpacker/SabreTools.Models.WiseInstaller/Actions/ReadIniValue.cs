namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Read INI Value
    /// 
    /// This action reads an entry from an existing .INI file into a variable. Example: Obtain a
    /// path to a file.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f8".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ReadIniValue : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Remove File Name (nonzero value)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Path to INI
        /// </summary>
        public string? Pathname { get; set; }

        /// <summary>
        /// INI section
        /// </summary>
        public string? Section { get; set; }

        /// <summary>
        /// Item key
        /// </summary>
        public string? Item { get; set; }

        /// <summary>
        /// Default value
        /// </summary>
        public string? DefaultValue { get; set; }
    }
}