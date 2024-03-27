using System;
using System.IO;

namespace WiseUnpacker.HWUN
{
    internal unsafe class BufferedFile
    {
        public Stream? FileHandle { get; private set; }
        public string? FileName { get; private set; }
        public byte[] Buffer { get; private set; } = new byte[0x8000]; // [$0000..$7fff]
        public uint Error { get; private set; }
        public uint bo { get; private set; }
        public uint FileSize { get; private set; }
        public uint FilePosition { get; private set; }

        public void Open(string s)
        {
            FileHandle = File.OpenRead(s);
            Buffer = new byte[0x8000];
            FileSize = (uint)FileHandle.Length;
            bo = 0xffff0000;
            FilePosition = 0x00000000;
        }

        public void Close()
        {
            Buffer = [];
            FileHandle?.Close();
        }

        public bool Valid(uint p)
        {
            return p >= 0 && p < FileSize;
        }

        public bool InMem(uint p, uint l)
        {
            return (p >= bo) && (p + 1 <= bo + 0x8000);
        }

        public bool EOF()
        {
            return FilePosition == FileSize;
        }

        public void FillBuffer(uint p)
        {
            bo = p - 0x4000;
            if (bo < 0x0000)
                bo = 0x0000;
            if (bo + 0x8000 > FileSize)
                bo = FileSize - 0x8000;

            // filesize <$8000
            if (bo < 0)
            {
                bo = 0;
                FileHandle!.Seek(bo, SeekOrigin.Begin);
                FileHandle.Read(Buffer!, 0, (int)FileSize);
            }
            else
            {
                FileHandle!.Seek(bo, SeekOrigin.Begin);
                FileHandle.Read(Buffer!, 0, 0x8000);
            }
            FilePosition = p;
        }

        public byte ReadByte()
            => ReadByte(FilePosition);

        public byte ReadByte(uint p)
        {
            byte res = 0;
            if (Valid(p))
            {
                if (!InMem(p, 1))
                    FillBuffer(p);

                res = Buffer![FilePosition - bo];
                FilePosition++;
            }
            else
            {
                Error = 1;
            }

            return res;
        }

        public ushort ReadWord()
            => ReadWord(FilePosition);

        public ushort ReadWord(uint p)
        {
            ushort res = 0;
            if (Valid(p))
            {
                if (!InMem(p, 2))
                    FillBuffer(p);

                res = BitConverter.ToUInt16(Buffer!, (int)(p - bo));
                FilePosition++;
            }
            else
            {
                Error = 1;
            }

            return res;
        }

        public uint ReadLongInt()
            => ReadLongInt(FilePosition);

        public uint ReadLongInt(uint p)
        {
            uint res = 0;
            if (Valid(p))
            {
                if (!InMem(p, 4))
                    FillBuffer(p);

                res = BitConverter.ToUInt32(Buffer!, (int)(p - bo));
                FilePosition++;
            }
            else
            {
                Error = 1;
            }

            return res;
        }

        public void Seek(uint p)
        {
            FilePosition = p;
        }
    }
}