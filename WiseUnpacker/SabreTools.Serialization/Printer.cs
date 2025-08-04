using System;
using System.Text;
using SabreTools.Serialization.Interfaces;
using SabreTools.Serialization.Printers;
using Wrapper = SabreTools.Serialization.Wrappers;

namespace SabreTools.Serialization
{
    /// <summary>
    /// Generic wrapper around printing methods
    /// </summary>
    public static class Printer
    {
        /// <summary>
        /// Print the item information from a wrapper to console as
        /// pretty-printed text
        /// </summary>
        public static void PrintToConsole(this IWrapper wrapper)
        {
            var sb = wrapper.ExportStringBuilderExt();
            if (sb == null)
            {
                Console.WriteLine("No item information could be generated");
                return;
            }

            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Export the item information as a StringBuilder
        /// </summary>
        public static StringBuilder? ExportStringBuilderExt(this IWrapper wrapper)
        {
            return wrapper switch
            {
                Wrapper.WiseOverlayHeader item => item.PrettyPrint(),
                Wrapper.WiseScript item => item.PrettyPrint(),
                _ => null,
            };
        }

#if NETCOREAPP
        /// <summary>
        /// Export the item information as JSON
        /// </summary>
        public static string ExportJSONExt(this IWrapper wrapper)
        {
            return wrapper switch
            {
                Wrapper.WiseOverlayHeader item => item.ExportJSON(),
                Wrapper.WiseScript item => item.ExportJSON(),
                _ => string.Empty,
            };
        }
#endif

        #region Static Printing Implementations

        /// <summary>
        /// Export the item information as pretty-printed text
        /// </summary>
        private static StringBuilder PrettyPrint(this Wrapper.WiseOverlayHeader item)
        {
            var builder = new StringBuilder();
            WiseOverlayHeader.Print(builder, item.Model);
            return builder;
        }

        /// <summary>
        /// Export the item information as pretty-printed text
        /// </summary>
        private static StringBuilder PrettyPrint(this Wrapper.WiseScript item)
        {
            var builder = new StringBuilder();
            WiseScript.Print(builder, item.Model);
            return builder;
        }

        #endregion
    }
}
