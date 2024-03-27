namespace WiseUnpacker.HWUN
{
    internal static class HexaDeci
    {
        public static string Hexa(byte[] L, byte B1)
        {
            string S; byte B0, B2;
            S = "";
            for (B0 = 1; B0 <= B1; B0++)
            {
                if (B0 % 2 == 1)
                    B2 = (byte)(L[B0] % 16);
                else
                    B2 = (byte)(L[B0] / 16);

                if (B2 < 0x0a)
                    S = (char)(0x30 + B2) + S;
                else
                    S = (char)(0x37 + B2) + S;
            }
            return S;
        }

        public static uint Deci(string S)
        {
            byte B; uint L;
            L = 0;
            for (B = 1; B < S.Length; B++)
            {
                if (((byte)S[B] >= 0x30) && ((byte)S[B] <= 0x39))
                    L = L * 16 + (byte)S[B] - 0x30;
                else if (((byte)char.ToUpperInvariant(S[B]) >= 0x41) && ((byte)char.ToUpperInvariant(S[B]) <= 0x46))
                    L = L * 16 + (byte)char.ToUpperInvariant(S[B]) - 0x37;
            }
            return L;
        }
    }
}