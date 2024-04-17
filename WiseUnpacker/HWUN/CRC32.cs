/// �����������������
/// CRC32 (TPU7-RM)   (c)2001 NNCI
/// �����������������
/// A low level unit for CRC32 calculation on every kind of data

namespace WiseUnpacker.HWUN
{
    internal static class CRC32
    {
        private const uint _polynomial = 0xEDB88320;

        private static readonly uint[] _table = new uint[256];

        static CRC32()
        {
            BuildTable();
        }

        public static uint Start()
        {
            unchecked { return (uint)-1; }
        }

        public static uint Add(uint crc, byte b)
        {
            // TODO: Determine why the 0x00FFFFFF mask is being applied
            return _table[(byte)(crc ^ b)] ^ ((crc >> 8) & 0x00FFFFFF);
        }

        public static uint Add(uint crc, byte[] buffer, ushort length)
        {
            if (length == 0)
                return crc;

            for (ushort i = 0; i < length; i++)
            {
                crc = _table[(byte)crc ^ buffer[i]] ^ (crc >> 8);
            }

            return crc;
        }

        public static uint End(uint crc)
        {
            unchecked { return crc ^ (uint)-1; };
        }

        private static void BuildTable()
        {
            for (int W0 = 0; W0 < 256; W0++)
            {
                uint crc = (uint)(W0 << 1);
                for (int W1 = 8; W1 > 0; W1--)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ _polynomial;
                    else
                        crc = crc >> 1;
                }

                _table[W0] = crc;
            }
        }
    }
}