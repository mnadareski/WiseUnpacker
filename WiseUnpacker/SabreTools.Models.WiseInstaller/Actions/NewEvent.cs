namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// New Event
    /// 
    /// Event scripts handle events. (Example: The end user cancels the installation.)
    /// 
    /// Mainline
    /// 
    ///     The primary script that’s executed during the normal installation process. It
    ///     contains placeholders for Cancel and Exit scripts. When you open a script, that script
    ///     is considered the “main installation script,” and is on the first tab below the
    ///     installation script.
    /// 
    /// Exit
    /// 
    ///     The script that’s executed when the installation is complete, or when an Exit
    ///     Installation script command is executed. If you create a user-defined action, you
    ///     store its custom dialog box here.
    /// 
    /// Cancel
    /// 
    ///     The script that’s executed when the end user cancels the installation. Because some
    ///     files might already be installed when the end user cancels, the Cancel script
    ///     contains the include script, rollback.wse, which returns the destination computer to
    ///     its pre-installation state.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class NewEvent : MachineStateData
    {
        /// <summary>
        /// Padding bytes
        /// </summary>
        /// <remarks>Either 0 or 6 bytes from samples</remarks>
        public byte[]? Padding { get; set; }
    }
}
