namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Custom Dialog
    /// 
    /// Use this action to create your own dialog box or dialog box set.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    /// TODO: Document the decompressed file format
    public class CustomDialogSet : MachineStateData
    {
        /// <summary>
        /// Start of the deflated data
        /// </summary>
        public uint DeflateStart { get; set; }

        /// <summary>
        /// End of the deflated data
        /// </summary>
        public uint DeflateEnd { get; set; }

        /// <summary>
        /// Inflated data size
        /// </summary>
        public uint InflatedSize { get; set; }

        /// <summary>
        /// Display variable name
        /// </summary>
        public string? DisplayVariable { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string? Name { get; set; }
    }
}
