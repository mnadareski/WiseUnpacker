using System;

namespace WiseUnpacker.HWUN
{
    internal static class HexaDeci
    {
        /// <summary>
        /// Convert a word value to hex string
        /// </summary>
        public static string Hexa(uint value)
            => Hexa(BitConverter.GetBytes(value), 4);

        /// <summary>
        /// Convert a byte array value to hex string
        /// </summary>
        public static string Hexa(byte[] value, byte length)
            => BitConverter.ToString(value, 0, length).Replace("-", string.Empty);

        /// <summary>
        /// Convert a hex string to a word value
        /// </summary>
        public static uint Deci(string value)
        {
            uint output = 0;

            value = value.ToUpperInvariant();
            for (byte i = 1; i < value.Length; i++)
            {
                // 0-9
                if ((value[i] >= 0x30) && (value[i] <= 0x39))
                    output = output * 16 + value[i] - 0x30;

                // A-Z
                else if (value[i] >= 0x41 && value[i] <= 0x46)
                    output = output * 16 + value[i] - 0x37;
            }

            return output;
        }
    }
}