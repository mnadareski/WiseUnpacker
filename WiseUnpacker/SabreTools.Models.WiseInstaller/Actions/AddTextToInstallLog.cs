namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Add Text to INSTALL.LOG
    /// 
    /// This action adds commands to the installation log (Install.log).
    /// 
    /// Use the Open/Close Install.log action to create the installation log.
    /// 
    /// As the installation runs on the destination computer, each action it performs is logged in
    /// the installation log (installation of files, additions or changes to registry, and so on).
    /// Failures are listed also, with the reason for failure. The uninstall reverses each action
    /// recorded in the Install.log, starting at the bottom of the log and going up. Typically, you
    /// add commands to the Install.log to customize the uninstall process for an application.
    /// 
    /// Because the log is written continuously during installation, the location of the text in the
    /// log depends on where in the script you place the Add Text to Install.log script line.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class AddTextToInstallLog : MachineStateData
    {
        /// <summary>
        /// Text
        /// </summary>
        public string? Text { get; set; }
    }
}
