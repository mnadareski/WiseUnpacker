namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Edit Registry
    /// 
    /// This action adds, edits, or deletes registry keys or values. You can create registry entries
    /// manually or import a registry file (.REG).
    /// </summary>
    /// <remarks>
    /// The documentation mentions that there are multiple options, including
    /// deleting keys and values, updating values, and adding new key-value
    /// pairs. As far as research has taken, this set of options does not
    /// appear to be immediately mappable to the data below.
    /// </remarks>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class EditRegistry : MachineStateData
    {
        /// <summary>
        /// Flags and Root
        /// </summary>
        /// <remarks>
        /// To get the root value, do (FlagsAndRoot & 0x1F)
        /// Flag values:
        /// - 0x10 - Force backup of registry value (if 0x80 == 0)
        ///     + It overrides the values set by 0x24 and 0x25
        ///       and then disables the flag after
        /// - 0x40 - Delete? (If root is not 0?)
        /// - 0x80 - Unknown
        /// </remarks>
        public byte FlagsAndRoot { get; set; }

        /// <summary>
        /// Data type, defaults to 0 if not defined
        /// in source scripts
        /// </summary>
        public byte DataType { get; set; }

        /// <summary>
        /// An unknown value that appears in some versions.
        /// Its presence indicates to load an external DLL
        /// for performing registry actions. Investigation
        /// is needed on how to determine the script is the
        /// version that uses this string or not.
        /// </summary>
        public string? UnknownFsllib { get; set; }

        /// <summary>
        /// Key path
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// New value
        /// </summary>
        public string? NewValue { get; set; }

        /// <summary>
        /// Value name
        /// </summary>
        public string? ValueName { get; set; }
    }
}
