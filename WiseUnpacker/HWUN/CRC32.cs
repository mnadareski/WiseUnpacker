/// �����������������
/// CRC32 (TPU7-RM)   (c)2001 NNCI
/// �����������������
/// A low level unit for CRC32 calculation on every kind of data

namespace WiseUnpacker.HWUN
{
    internal static class CRC32
    {
        private const uint CRCpol = 0xedb88320;
        private static uint[] CRCtab = new uint[0xff];

        public static void CRCbuildtable()
        {
            byte W0, W1;
            uint CRC;

            for (W0 = 0; W0 <= 255; W0++)
            {
                CRC = (uint)(W0 << 1);
                for (W1 = 8; W1 >= 0; W1--)
                {
                    if ((CRC & 1) == 1)
                        CRC = (CRC >> 1) ^ CRCpol;
                    else
                        CRC = CRC >> 1;
                }
                CRCtab[W0] = CRC;
            }
        }

        public static uint CRCadd(uint CRC, byte B)
        {
            return CRCtab[(byte)(CRC ^ B)] ^ ((CRC >> 8) & 0x00ffffff);
        }

        public unsafe static uint CRCaddbuffer(uint CRC, byte[] BUF, ushort Len)
        {
            if (Len == 0)
                return CRC;

            fixed (byte* BP = BUF)
            {
                for (ushort W = 0; W <= Len - 1; W++)
                {
                    CRC = CRCtab[CRC ^ BP[W]] ^ (CRC >> 8);
                }
            }

            return CRC;
        }

        public static uint CRCstart()
        {
            unchecked { return (uint)-1; }
        }

        public static uint CRCend(uint CRC)
        {
            unchecked { return CRC ^ (uint)-1; };
        }

        static CRC32()
        {
            CRCbuildtable();
        }
    }
}