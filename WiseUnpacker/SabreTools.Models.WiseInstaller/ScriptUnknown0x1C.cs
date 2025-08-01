namespace SabreTools.Models.WiseInstaller
{
    /// <remarks>
    /// Examples:
    ///   0x1C 'RegDB Tree: Software\Sierra OnLine\Setup\GUNMANDEMO
    ///   0x1C 'RegDB Root: 2
    ///   0x1C 'RegDB Tree: Software\Sierra On-Line\Gunman Demo
    ///   0x1C 'RegDB Root: 2
    ///   0x1C 'RegDB Tree: Software\Valve\Gunman Demo
    ///   0x1C 'RegDB Root: 1
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptUnknown0x1C : MachineStateData
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public string? UnknownString_1 { get; set; }
    }
}
