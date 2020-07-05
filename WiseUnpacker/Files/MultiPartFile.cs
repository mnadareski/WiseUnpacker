using System;
using System.IO;
using System.Text;

namespace WiseUnpacker.Files
{
    internal class MultiPartFile
    {
        private Stream fileStream;
        private long partStart;
        private long partEnd;
        private MultiPartFile next;

        public long Position { get; private set; }
        public long Length { get; private set; }

        private class MultiPartFileBuffer
        {
            public byte[] Buffer = new byte[0x8000];
            public int Position;
            public int Size;
            public int End;
        }

        public MultiPartFile(string name)
        {
            fileStream = File.OpenRead(name);
            partStart = 0;
            Position = 0;
            partEnd = fileStream.Length - 1;
            Length = partEnd + 1;
            next = null;
        }

        public bool Append(string name)
        {
            try
            {
                MultiPartFile mf = this;
                while (next != null)
                {
                    mf = mf.next;
                }

                mf.next = new MultiPartFile(name);
                mf.next.partStart = Length;
                mf.next.partEnd += Length;
                Length = mf.next.partEnd + 1;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Seek(long pos)
        {
            Position = pos;
        }

        public void Close()
        {
            if (next != null)
                next.Close();

            fileStream.Close();
        }

        public bool Read(byte[] x, int offset, int amount)
        {
            int bufpos;

            MultiPartFile mf = this;
            while (Position > mf.partEnd && mf.next != null)
                mf = mf.next;

            if (Position <= mf.partEnd)
            {
                mf.fileStream.Seek(Position - mf.partStart, SeekOrigin.Begin);
                if (mf.partEnd + 1 - Position >= amount)
                {
                    mf.fileStream.Read(x, offset, amount);
                    Position += amount;
                }
                else
                {
                    byte[] buf = new byte[0xffff];
                    bufpos = 0;
                    do
                    {
                        if (mf.partEnd + 1 < Position + amount - bufpos)
                        {
                            mf.fileStream.Read(buf, bufpos, (int)(mf.partEnd + 1 - Position));
                            bufpos += (int)(mf.partEnd + 1 - Position);
                            Position = mf.partEnd + 1;
                            mf = mf.next;
                        }
                        else
                        {
                            mf.fileStream.Read(buf, bufpos, amount - bufpos);
                            Position += amount - bufpos;
                            bufpos = amount;
                        }
                    }
                    while (bufpos != amount);

                    Array.ConstrainedCopy(buf, 0, x, offset, amount);
                }

                return true;
            }

            return false;
        }

        public byte ReadByte()
        {
            byte[] x = new byte[1];
            Read(x, 0, 1);
            return x[0];
        }

        public byte[] ReadBytes(int count)
        {
            byte[] x = new byte[count];
            Read(x, 0, count);
            return x;
        }

        public char ReadChar()
        {
            byte[] x = new byte[1];
            Read(x, 0, 1);
            return (char)x[0];
        }

        public char[] ReadChars(int count)
        {
            byte[] x = new byte[count];
            Read(x, 0, count);
            return Encoding.Default.GetString(x).ToCharArray();
        }

        public short ReadInt16()
        {
            byte[] x = new byte[2];
            Read(x, 0, 2);
            return BitConverter.ToInt16(x, 0);
        }

        public ushort ReadUInt16()
        {
            byte[] x = new byte[2];
            Read(x, 0, 2);
            return BitConverter.ToUInt16(x, 0);
        }

        public int ReadInt32()
        {
            byte[] x = new byte[4];
            Read(x, 0, 4);
            return BitConverter.ToInt32(x, 0);
        }

        public uint ReadUInt32()
        {
            byte[] x = new byte[4];
            Read(x, 0, 4);
            return BitConverter.ToUInt32(x, 0);
        }

        public long ReadInt64()
        {
            byte[] x = new byte[8];
            Read(x, 0, 8);
            return BitConverter.ToInt64(x, 0);
        }

        public ulong ReadUInt64()
        {
            byte[] x = new byte[8];
            Read(x, 0, 8);
            return BitConverter.ToUInt64(x, 0);
        }
    }
}
