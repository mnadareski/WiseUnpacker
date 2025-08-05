namespace SabreTools.Models.WiseInstaller
{
    /// <remarks>
    /// Every instance of the string has been a .lnk file
    /// with no apparent destination. It seems to be acting
    /// like a check space available or a `touch` command,
    /// since they mostly occur before a copy function.
    /// 
    /// There is one instance where it is doing a path with
    /// a wildcard. This may mean this is a delete command.
    /// The issue with this theory is that there do not seem
    /// to be any flags, notably missing the "Include Sub Directories"
    /// and "Remove Directory Containing Files" items.
    /// 
    /// There's already another definition for delete file,
    /// so that just muddies the waters about what this is
    /// supposed to be further.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ScriptUnknown0x11 : MachineStateData
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public string? Operand_1 { get; set; }
    }
}
