namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Set Files/Buffers
    /// 
    /// This action sets the FILES= and BUFFERS= lines in Config.sys. If either is currently
    /// lower than the minimum specified in this action, it is increased to the specified value. If
    /// either is already greater than the minimum specified in this action, it is not changed.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f21".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class SetFilesBuffers : FunctionData
    {
        /// <summary>
        /// Minimum files to be specfied in FILES= in CONFIG.SYS
        /// </summary>
        /// <remarks>Blank means leave unchanged</remarks>
        public string? MinimumFiles { get; set; }

        /// <summary>
        /// Minimum buffers to be specfied in BUFFERS= in CONFIG.SYS
        /// </summary>
        /// <remarks>Blank means leave unchanged</remarks>
        public string? MinimumBuffers { get; set; }
    }
}