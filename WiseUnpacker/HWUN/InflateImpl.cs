using System.IO;
using SabreTools.IO;

namespace WiseUnpacker.HWUN
{
    internal class InflateImpl : SecureInflate
    {
        public ReadOnlyCompositeStream? Input { get; private set; }
        public Stream? Output { get; private set; }
        public byte[] InputBuffer { get; private set; } = new byte[0x4000];
        public ushort InputBufferPosition { get; private set; }
        public ushort InputBufferSize { get; private set; }
        public uint InputSize { get; private set; }
        public uint OutputSize { get; private set; }
        public ushort Result { get; private set; }
        public uint CRC { get; set; }

        public bool Inflate(ReadOnlyCompositeStream inf, string outf)
        {
            InputBuffer = new byte[0x4000];
            Input = inf;
            Output = File.OpenWrite(outf);
            InputSize = 0;
            OutputSize = 0;
            InputBufferSize = (ushort)InputBuffer.Length;
            InputBufferPosition = InputBufferSize;

            CRC = CRC32.Start();
            bool inflated = SI_INFLATE();
            inf.Seek(inf.Position - InputBufferSize + InputBufferPosition, SeekOrigin.Begin);
            CRC = CRC32.End(CRC);

            Result = SI_ERROR;
            Output.Close();
            InputBuffer = [];
            return inflated;
        }

        public override byte? SI_READ()
        {
            if (InputBufferPosition >= InputBufferSize)
            {
                if (Input!.Position >= Input!.Length)
                {
                    SI_BREAK = true;
                    return null;
                }
                else
                {
                    if (InputBufferSize > Input!.Length - Input!.Position)
                        InputBufferSize = (ushort)(Input!.Length - Input!.Position);

                    Input!.Read(InputBuffer, 0, InputBufferSize);
                    InputBufferPosition = 0x0000;
                }
            }

            byte inflateread = InputBuffer[InputBufferPosition];
            InputSize++;
            InputBufferPosition++;
            return inflateread;
        }

        public override void SI_WRITE(ushort amount)
        {
            OutputSize += amount;
            Output!.Write(SI_WINDOW, 0, amount);
            CRC = CRC32.Add(CRC, SI_WINDOW, amount);
        }
    }
}