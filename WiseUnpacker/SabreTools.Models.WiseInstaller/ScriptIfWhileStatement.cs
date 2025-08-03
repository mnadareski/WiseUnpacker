namespace SabreTools.Models.WiseInstaller
{
    /// <summary>
    /// If/While statement data
    /// </summary>
    /// <remarks>
    /// Examples:
    ///   0x0C 18 'DELAY' '3000'
    ///   0x0C 00 'LANG' 'Deutsch'
    ///   0x0C 00 'BRANDING' '1'
    ///   0x0C 00 'NAME' '(null)'
    ///   0x0C 07 'WOLF_VERSION' '1.32'
    ///   0x0C 00 'WOLF_VERSION' '(null)'
    ///   0x0C 00 'PATCH_INSTALLED' '1'
    ///   0x0C 0A 'COMPONENTS' 'B'
    ///   0x0C 00 'DISPLAY' 'Start Installation'
    ///   0x0C 00 'DIRECTION' 'N'
    ///   0x0C 02 'MAINDIR' '('
    ///   0x0C 02 'MAINDIR' '?'
    ///   0x0C 02 'MAINDIR' '/'
    ///   0x0C 02 'THE_PATH' ':'
    ///   0x0C 02 'MAINDIR' '*'
    ///   0x0C 02 'MAINDIR' '"'
    ///   0x0C 02 'MAINDIR' '<'
    ///   0x0C 02 'MAINDIR' '>'
    ///   0x0C 02 'MAINDIR' '|'
    ///   0x0C 02 'MAINDIR' ';'
    ///   0x0C 02 'MAINDIR' ')'
    ///   0x0C 04 'MAINDIR' '%WIN%'
    ///   0x0C 0A 'COMPONENTS' 'A'
    ///   0x0C 00 'LANG' 'English'
    ///   0x0C 00 'LANG' 'Deutsch'
    ///   0x0C 00 'LANG' 'Italiano'
    ///   0x0C 0A 'COMPONENTS' 'B'
    ///   0x0C 00 'PATCH_INSTALLED' '1'
    ///   0x0C 00 'COMPONENTS' 'A'
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    public class ScriptIfWhileStatement : MachineStateData
    {
        /// <summary>
        /// Flags, unknown values
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string? Variable { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string? Value { get; set; }
    }
}
