namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Get Registry Key Value
    /// 
    /// This action puts the value of a registry key into a variable. Multi-line (MULTI_SZ)
    /// registry values are read into a list format.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f9".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class GetRegistryKeyValue : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Remove File Name (unknown)
        /// - Expand Environment Variables (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Registry key
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Default value if not found
        /// </summary>
        public string? Default { get; set; }

        /// <summary>
        /// Value name, blank for Win16
        /// </summary>
        public string? ValueName { get; set; }

        /// <summary>
        /// The root that contains the registry key.
        /// </summary>
        public string? Root { get; set; }
    }
}