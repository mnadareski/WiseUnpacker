namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// Section definition
    /// </summary>
    /// <remarks>
    /// Examples:
    ///   0x0B 00 '%MAINDIR%\cgame_mp_x86.dll'
    ///   0x0B 00 '%MAINDIR%\qagame_mp_x86.dll'
    ///   0x0B 00 '%MAINDIR%\ui_mp_x86.dll'
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptUnknown0x0B : MachineStateData
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public byte Unknown_1 { get; set; }

        /// <summary>
        /// File name?
        /// </summary>
        public string? UnknownString_1 { get; set; }
    }
}
