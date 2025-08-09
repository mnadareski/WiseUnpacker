namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Get Environment Variable
    /// 
    /// This action puts the value of a Windows environment variable into a WiseScript variable.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f17".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class GetEnvironmentVariable : FunctionData
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Remove File Name (non-zero value)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Environment variable name
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Optional default value used if environment variable
        /// is not found
        /// </summary>
        public string? DefaultValue { get; set; }
    }
}