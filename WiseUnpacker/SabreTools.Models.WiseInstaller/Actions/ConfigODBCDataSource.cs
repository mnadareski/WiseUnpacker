namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Config ODBC Data Source
    /// 
    /// This action configures an ODBC data source for use with an existing ODBC (Open
    /// Database Connectivity) driver.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/> 
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class ConfigODBCDataSource : MachineStateData
    {
        /// <summary>
        /// Flags
        /// </summary>
        /// <remarks>
        /// Expected flags:
        /// - System (unknown)
        /// - Display Message Only (unknown)
        /// - Abort Installation (unknown)
        /// - Start Block (unknown)
        /// </remarks>
        public byte Flags { get; set; }

        /// <summary>
        /// File format string
        /// </summary>
        /// <remarks>
        /// Formatted like "File Description (*.ext)"
        /// </remarks>
        public string? FileFormat { get; set; }

        /// <summary>
        /// Connection string for the data source
        /// </summary>
        /// <remarks>
        /// Contains the following fields in order, separated
        /// by 0x7F characters, similar to function calls:
        /// - DSN=: Driver Name
        /// - DSN=: Data Source Name
        /// - DESCRIPTION=: Description of the data source
        /// - DBQ=: Path to the database
        /// </remarks>
        public string? ConnectionString { get; set; }
    }
}
