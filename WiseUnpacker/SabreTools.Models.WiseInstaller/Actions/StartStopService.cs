namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Start/Stop Service
    /// 
    /// This action lets you start or stop a service on the destination computer. It only applies to
    /// operating systems that support services.
    /// 
    /// When a WiseScript is called by a Windows Installer installation, you can also start and
    /// stop services by using the Services page in Windows Installer Editor.
    /// 
    /// After you try to stop a service, the script pauses to give the service time to stop. The
    /// currently logged-in end user must have the appropriate privileges to start and stop
    /// services.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f36".
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class StartStopService
    {
        /// <summary>
        /// Operation
        /// </summary>
        /// <remarks>
        /// Stop = 0, Start = 1
        /// </remarks>
        public byte Operation { get; set; }

        /// <summary>
        /// Internal name of the service to start or stop
        /// </summary>
        public string? ServiceName { get; set; }
    }
}