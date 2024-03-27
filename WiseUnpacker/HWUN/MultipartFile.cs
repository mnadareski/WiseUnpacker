using System;
using System.IO;

namespace WiseUnpacker.HWUN
{
    internal class MultipartFile
    {
        public Stream? FileHandle { get; private set; }
        public uint FileSize { get; private set; }
        public uint FilePosition { get; private set; }
        public uint FileEnd { get; private set; }
        public uint FileLength { get; private set; }
        public MultipartFile? Next { get; private set; }

        public static bool Open(string name, out MultipartFile? mf)
        {
            try
            {
                mf = new MultipartFile();

                mf.FileHandle = File.OpenRead(name);
                mf.FileSize = 0;
                mf.FilePosition = 0;
                mf.FileEnd = (uint)(mf.FileHandle.Length - 1);
                mf.FileLength = mf.FileEnd + 1;
                mf.Next = null;

                return true;
            }
            catch
            {
                mf = null;
                return false;
            }
        }

        public bool Append(string name)
        {
            MultipartFile mf = this;
            while (mf.Next != null)
            {
                mf = mf.Next;
            }

            if (Open(name, out MultipartFile? next))
            {
                mf.Next = next;
                mf.Next!.FileSize = FileLength;
                mf.Next.FileEnd += FileLength;
                FileLength = mf.Next.FileEnd + 1;
                return true;
            }
            else
            {
                mf.Next = null;
                return false;
            }
        }

        public void Seek(uint pos)
        {
            FilePosition = pos;
        }

        public void Close()
        {
            Next?.Close();
            FileHandle?.Close();
        }

        public bool BlockRead(byte[] buffer, ushort amount)
        {
            MultipartFile mf = this;
            while (FilePosition > mf.FileEnd && mf.Next != null)
            {
                mf = mf.Next;
            }

            if (FilePosition > mf.FileEnd)
                return false;

            mf.FileHandle!.Seek(FilePosition - mf.FileSize, SeekOrigin.Begin);
            if (mf.FileEnd + 1 - FilePosition > amount)
            {
                mf.FileHandle.Read(buffer, 0, amount);
                FilePosition += amount;
            }
            else
            {
                byte[] buf = new byte[0xffff];
                uint bufpos = 0;
                do
                {
                    if (mf!.FileEnd + 1 < FilePosition + amount - bufpos)
                    {
                        mf.FileHandle!.Read(buf, (int)bufpos, (int)(mf.FileEnd + 1 - FilePosition));
                        bufpos += mf.FileEnd + 1 - FilePosition;
                        FilePosition = mf.FileEnd + 1;
                        mf = mf.Next!;
                    }
                    else
                    {
                        mf.FileHandle!.Read(buf, (int)bufpos, (int)(amount - bufpos));
                        FilePosition += amount - bufpos;
                        bufpos = amount;
                    }
                } while (bufpos != amount);

                Array.Copy(buf, buffer, amount);
            }

            return true;
        }
    }
}