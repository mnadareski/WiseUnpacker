namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Self-Register OCXs/DLLs
    /// 
    /// Use this action to self-register all queued .OCX, .DLL, and .EXE files or to add an existing
    /// file to the queue.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f29".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class SelfRegisterOCXsDLLs
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Register all pending OCXs/DLLs/EXEs (unknown)
        /// - Queue existing file for self-registration
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// Description/Pathname
        /// </summary>
        public string? Description { get; set; }
    }
}