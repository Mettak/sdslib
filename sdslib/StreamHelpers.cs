using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace sdslib
{
    /// <summary>
    /// Provides help functions for browsing SDS files
    /// </summary>
    public static class StreamHelpers
    {
        public static byte[] ReadAllBytes(this Stream stream)
        {
            byte[] array = new byte[stream.Length];
            stream.SeekToStart();
            stream.Read(array, 0, array.Length);
            return array;
        }

        public static byte[] ReadBytes(this Stream stream, int count)
        {
            byte[] array = new byte[count];
            stream.Read(array, 0, array.Length);
            return array;
        }

        public static byte ReadUInt8(this Stream stream)
        {
            return (byte)stream.ReadByte();
        }

        public static ushort ReadUInt16(this Stream stream)
        {
            byte[] array = new byte[Constants.DataTypesSizes.UInt16];
            stream.Read(array, 0, array.Length);
            return BitConverter.ToUInt16(array, 0);
        }

        public static uint ReadUInt32(this Stream stream)
        {
            byte[] array = new byte[Constants.DataTypesSizes.UInt32];
            stream.Read(array, 0, array.Length);
            return BitConverter.ToUInt32(array, 0);
        }

        public static ulong ReadUInt64(this Stream stream)
        {
            byte[] array = new byte[Constants.DataTypesSizes.UInt64];
            stream.Read(array, 0, array.Length);
            return BitConverter.ToUInt64(array, 0);
        }

        public static string ReadString(this Stream stream, int count)
        {
            byte[] array = new byte[count];
            stream.Read(array, 0, array.Length);
            return Encoding.ASCII.GetString(array).Replace("\0", string.Empty);
        }

        public static void SeekToStart(this Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        public static void Write(this Stream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

        public static void WriteString(this Stream stream, string text)
        {
            stream.Write(Encoding.ASCII.GetBytes(text), 0, text.Length);
        }

        public static void WriteString(this Stream stream, string text, int count)
        {
            if (count > text.Length)
            {
                int diff = count - text.Length;
                for (int i = 0; i < diff; i++) text += "\0";
            }

            stream.Write(Encoding.ASCII.GetBytes(text), 0, count);
        }

        public static void WriteUInt8(this Stream stream, byte uint8)
        {
            stream.Write(BitConverter.GetBytes(uint8), 0, Constants.DataTypesSizes.UInt8);
        }

        public static void WriteUInt16(this Stream stream, ushort uint16)
        {
            stream.Write(BitConverter.GetBytes(uint16), 0, Constants.DataTypesSizes.UInt16);
        }

        public static void WriteUInt32(this Stream stream, uint uint32)
        {
            stream.Write(BitConverter.GetBytes(uint32), 0, Constants.DataTypesSizes.UInt32);
        }

        public static void WriteUInt64(this Stream stream, ulong uint64)
        {
            stream.Write(BitConverter.GetBytes(uint64), 0, Constants.DataTypesSizes.UInt64);
        }

        public static string ReadStringDynamic(this Stream stream, Encoding encoding, char end)
        {
            int characterSize = encoding.GetByteCount("e");
            string characterEnd = end.ToString(CultureInfo.InvariantCulture);

            int i = 0;
            var data = new byte[128 * characterSize];

            while (true)
            {
                if (i + characterSize > data.Length)
                {
                    Array.Resize(ref data, data.Length + (128 * characterSize));
                }
                
                stream.Read(data, i, characterSize);

                if (encoding.GetString(data, i, characterSize) == characterEnd)
                {
                    break;
                }

                i += characterSize;
            }

            if (i == 0)
            {
                return string.Empty;
            }

            return encoding.GetString(data, 0, i);
        }

        public static string ReadStringDynamic(this Stream stream)
        {
            return stream.ReadStringDynamic(Encoding.UTF8, '\0');
        }
    }
}
