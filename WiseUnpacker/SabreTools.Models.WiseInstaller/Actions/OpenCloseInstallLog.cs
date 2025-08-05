namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Open/Close Install.log
    /// 
    /// Use this action to create an installation log.
    /// Normally, every file that is installed is recorded in the install.log. The uninstall works by
    /// reading Install.log from bottom to top and reversing each recorded action.
    /// 
    /// The Open/Close Install.log action lets you customize the uninstall, by turning logging off
    /// and on at key points to prevent some actions from being recorded in the log. If you use
    /// this action to stop logging, you must also use it to resume logging or no log file is
    /// created.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class OpenCloseInstallLog : MachineStateData
    {
        /// <summary>
        /// Flags, unknown values
        /// </summary>
        /// <remarks>
        /// Expected flags:
        /// - Resume/Start writing entries into installation log (0x01/0x02?)
        /// - Pause writing entries into installation log (0x00)
        /// - Open new installation log (0x01/0x02?)
        /// </remarks>
        public ushort Flags { get; set; }

        /// <summary>
        /// Log name used when opening a new log
        /// </summary>
        public string? LogName { get; set; }
    }
}
