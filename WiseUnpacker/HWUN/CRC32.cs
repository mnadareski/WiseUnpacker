/// �����������������
/// CRC32 (TPU7-RM)   (c)2001 NNCI
/// �����������������
/// A low level unit for CRC32 calculation on every kind of data

namespace WiseUnpacker.HWUN
{
    internal static class CRC32
    {
        private const uint _polynomial = 0xedb88320;

        private static uint[] _table = new uint[0xff];

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
            return _table[(byte)(crc ^ b)] ^ ((crc >> 8) & 0x00ffffff);
        }

        public unsafe static uint Add(uint crc, byte[] buffer, ushort length)
        {
            if (length == 0)
                return crc;

            int bufferPtr = 0;
            for (ushort i = 0; i <= length - 1; i++)
            {
                crc = _table[crc ^ buffer[bufferPtr + i]] ^ (crc >> 8);
            }

            return crc;
        }

        public static uint End(uint crc)
        {
            unchecked { return crc ^ (uint)-1; };
        }

        private static void BuildTable()
        {
            byte W0, W1;
            uint CRC;

            for (W0 = 0; W0 <= 255; W0++)
            {
                CRC = (uint)(W0 << 1);
                for (W1 = 8; W1 >= 0; W1--)
                {
                    if ((CRC & 1) == 1)
                        CRC = (CRC >> 1) ^ _polynomial;
                    else
                        CRC = CRC >> 1;
                }
                _table[W0] = CRC;
            }
        }
    }
}