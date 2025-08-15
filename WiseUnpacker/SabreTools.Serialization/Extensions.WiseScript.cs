using System.Text.RegularExpressions;

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
                "f0" => "Add Directory to PATH",
                "f1" => "Add to AUTOEXEC.BAT",
                "f2" => "Add to CONFIG.SYS",
                "f3" => "Add to SYSTEM.INI",
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
                "f20" => "Set File Attributes",
                "f21" => "Set Files/Buffers",
                "f22" => "Find File in Path",
                "f23" => "Check Disk Space",
                "f25" => "Insert Line Into Text File",
                "f27" => "Parse String",
                "f28" => "Exit Installation",
                "f29" => "Self-Register OCXs/DLLs",
                "f30" => "Install DirectX Components",
                "f31" => "Wizard Block",
                "f33" => "Read/Update Text File",
                "f34" => "Post to HTTP Server",
                "f35" => "Prompt for Filename",
                "f36" => "Start/Stop Service",
                "f38" => "Check HTTP Connection",

                // Undefined function IDs
                "f4" => $"UNDEFINED {functionId}",
                "f5" => $"UNDEFINED {functionId}",
                "f6" => $"UNDEFINED {functionId}",
                "f7" => $"UNDEFINED {functionId}",
                "f14" => $"UNDEFINED {functionId}",
                "f18" => $"UNDEFINED {functionId}",
                "f24" => $"UNDEFINED {functionId}",
                "f26" => $"UNDEFINED {functionId}",
                "f32" => $"UNDEFINED {functionId}",
                "f37" => $"UNDEFINED {functionId}",

                // External DLL
                null => null,
                _ => Regex.IsMatch(functionId, @"^f[0-9]{1,2}$") ? $"UNDEFINED {functionId}" : $"External: {functionId}",
            };
        }
    }
}