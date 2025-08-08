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
        /// - 0x40 - Delete? (If root is not 0?)
        /// - 0x80 - Unknown
        /// </remarks>
        public byte FlagsAndRoot { get; set; }

        /// <summary>
        /// Data type, defaults to 0 if not defined
        /// in source scripts
        /// </summary>
        /// <remarks>
        /// This value is a byte in most versions of the WiseScript
        /// format, but seems to be a ushort in newer(?) versions.
        /// It is unknown if this is version-controlled or
        /// flag-controlled, but it is difficult to tell what uses
        /// which format.
        /// 
        /// One version of WISE0001.DLL has the length as 2 bytes,
        /// notably from an installer that is not that old. In the
        /// final check, it only seems to check the first byte.
        /// </remarks>
        public ushort DataType { get; set; }

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
