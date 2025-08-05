namespace SabreTools.Serialization
{
    public static partial class Extensions
    {
        /// <summary>
        /// Convert a Wise function ID to the formal action name
        /// </summary>
        /// <param name="functionId">Function ID to convert</param>
        /// <returns>The formal action name on success, null otherwise</returns>
        public static string? FromWiseFunctionId(this string? functionId)
        {
            return functionId switch
            {
                "f1" => "Unknown",
                "f8" => "Read INI Value",
                "f9" => "Get Registry Key Value",
                "f10" => "Register Font",
                "f11" => "Win32 System Directory",
                "f12" => "Check Configuration",
                "f13" => "Search for File",
                "f15" => "Read/Write Binary File",
                "f16" => "Set Variable",
                "f17" => "Get Environment Variable",
                "f19" => "Check if File/Dir Exists",
                "f20" => "Set File Attributes(?)",
                "f22" => "Find File in Path",
                "f23" => "Check Disk Space",
                "f25" => "Insert Line Into Text File",
                "f27" => "Parse String",
                "f28" => "Unknown",
                "f29" => "Self-Register OCXs/DLLs",
                "f30" => "Unknown",
                "f31" => "Wizard Block",
                "f33" => "Read/Update Text File",
                "f34" => "Post to HTTP Server",

                // External DLL
                "ShellLink" => "Create Shortcut",
                _ => null,
            };
        }
    }
}