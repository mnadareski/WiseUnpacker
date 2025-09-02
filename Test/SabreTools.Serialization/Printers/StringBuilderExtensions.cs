using System;
using System.Text;

namespace SabreTools.Serialization.Printers
{
    // TODO: Add extension for printing enums, if possible
    internal static class StringBuilderExtensions
    {
        /// <summary>
        /// Append a line containing a boolean to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, bool? value, string prefixString)
        {
            value ??= false;
            return sb.AppendLine($"{prefixString}: {value}");
        }

        /// <summary>
        /// Append a line containing a Char to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, char? value, string prefixString)
        {
            string valueString = (value == null ? "[NULL]" : value.Value.ToString());
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Int8 to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, sbyte? value, string prefixString)
        {
            value ??= 0;
            string valueString = $"{value} (0x{value:X2})";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a UInt8 to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, byte? value, string prefixString)
        {
            value ??= 0;
            string valueString = $"{value} (0x{value:X2})";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Int16 to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, short? value, string prefixString)
        {
            value ??= 0;
            string valueString = $"{value} (0x{value:X4})";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a UInt16 to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, ushort? value, string prefixString)
        {
            value ??= 0;
            string valueString = $"{value} (0x{value:X4})";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Int32 to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, int? value, string prefixString)
        {
            value ??= 0;
            string valueString = $"{value} (0x{value:X8})";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a UInt32 to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, uint? value, string prefixString)
        {
            value ??= 0;
            string valueString = $"{value} (0x{value:X8})";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Single to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, float? value, string prefixString)
        {
            value ??= 0;
            string valueString = $"{value}";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Int64 to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, long? value, string prefixString)
        {
            value ??= 0;
            string valueString = $"{value} (0x{value:X16})";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a UInt64 to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, ulong? value, string prefixString)
        {
            value ??= 0;
            string valueString = $"{value} (0x{value:X16})";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Double to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, double? value, string prefixString)
        {
            value ??= 0;
            string valueString = $"{value}";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a string to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, string? value, string prefixString)
        {
            string valueString = value ?? "[NULL]";
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Guid to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, Guid? value, string prefixString)
        {
            value ??= Guid.Empty;
            string valueString = value.Value.ToString();
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a UInt8[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, byte[]? value, string prefixString)
        {
            string valueString = (value == null ? "[NULL]" : BitConverter.ToString(value).Replace('-', ' '));
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a UInt8[] value as a string to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, byte[]? value, string prefixString, Encoding encoding)
        {
            string valueString = (value == null ? "[NULL]" : encoding.GetString(value).Replace("\0", string.Empty));
            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Char[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, char[]? value, string prefixString)
        {
            string valueString = "[NULL]";
            if (value != null)
            {
                var valueArr = Array.ConvertAll(value, c => c.ToString());
                valueString = string.Join(", ", valueArr);
            }

            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Int16[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, short[]? value, string prefixString)
        {
            string valueString = "[NULL]";
            if (value != null)
            {
                var valueArr = Array.ConvertAll(value, s => s.ToString());
                valueString = string.Join(", ", valueArr);
            }

            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a UInt16[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, ushort[]? value, string prefixString)
        {
            string valueString = "[NULL]";
            if (value != null)
            {
                var valueArr = Array.ConvertAll(value, u => u.ToString());
                valueString = string.Join(", ", valueArr);
            }

            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Int32[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, int[]? value, string prefixString)
        {
            string valueString = "[NULL]";
            if (value != null)
            {
                var valueArr = Array.ConvertAll(value, i => i.ToString());
                valueString = string.Join(", ", valueArr);
            }

            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a UInt32[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, uint[]? value, string prefixString)
        {
            string valueString = "[NULL]";
            if (value != null)
            {
                var valueArr = Array.ConvertAll(value, u => u.ToString());
                valueString = string.Join(", ", valueArr);
            }

            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Single[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, float[]? value, string prefixString)
        {
            string valueString = "[NULL]";
            if (value != null)
            {
                var valueArr = Array.ConvertAll(value, u => u.ToString());
                valueString = string.Join(", ", valueArr);
            }

            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Int64[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, long[]? value, string prefixString)
        {
            string valueString = "[NULL]";
            if (value != null)
            {
                var valueArr = Array.ConvertAll(value, l => l.ToString());
                valueString = string.Join(", ", valueArr);
            }

            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a UInt64[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, ulong[]? value, string prefixString)
        {
            string valueString = "[NULL]";
            if (value != null)
            {
                var valueArr = Array.ConvertAll(value, u => u.ToString());
                valueString = string.Join(", ", valueArr);
            }

            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a Double[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, double[]? value, string prefixString)
        {
            string valueString = "[NULL]";
            if (value != null)
            {
                var valueArr = Array.ConvertAll(value, u => u.ToString());
                valueString = string.Join(", ", valueArr);
            }

            return sb.AppendLine($"{prefixString}: {valueString}");
        }

        /// <summary>
        /// Append a line containing a UInt64[] value to a StringBuilder
        /// </summary>
        public static StringBuilder AppendLine(this StringBuilder sb, Guid[]? value, string prefixString)
        {
            string valueString = "[NULL]";
            if (value != null)
            {
                var valueArr = Array.ConvertAll(value, g => g.ToString());
                valueString = string.Join(", ", valueArr);
            }

            return sb.AppendLine($"{prefixString}: {valueString}");
        }
    }
}
