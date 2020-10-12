using System;
using System.Collections.Generic;
using System.Text;

namespace sdslib
{
    public static class FNV
    {
        private static byte[] GetBytes(params object[] data)
        {
            List<byte> bufferList = new List<byte>();
            foreach (var obj in data)
            {
                if (obj == null)
                {
                    continue;
                }

                switch (obj.GetType().Name)
                {
                    case "String":
                        bufferList.AddRange(Encoding.UTF8.GetBytes(obj as string));
                        break;

                    case "Byte":
                        bufferList.Add(Convert.ToByte(obj));
                        break;

                    case "UInt16":
                        bufferList.AddRange(BitConverter.GetBytes(Convert.ToUInt16(obj)));
                        break;

                    case "UInt32":
                        bufferList.AddRange(BitConverter.GetBytes(Convert.ToUInt32(obj)));
                        break;

                    case "UInt64":
                        bufferList.AddRange(BitConverter.GetBytes(Convert.ToUInt64(obj)));
                        break;

                    default:
                        throw new NotSupportedException(obj.GetType().Name);
                }
            }

            byte[] buffer = bufferList.ToArray();
            return buffer;
        }

        public static uint Hash32(byte[] buffer)
        {
            uint hash = 2166136261U;
            for (int i = 0; i < buffer.Length; i++)
            {
                hash *= 16777619u;
                hash ^= (uint)buffer[i];
            }

            return hash;
        }

        public static uint Hash32(params object[] data)
        {
            return Hash32(GetBytes(data));
        }

        public static ulong Hash64(byte[] buffer)
        {
            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < buffer.Length; i++)
            {
                hash *= 1099511628211UL;
                hash ^= (ulong)buffer[i];
            }

            return hash;
        }

        public static ulong Hash64(params object[] data)
        {
            return Hash64(GetBytes(data));
        }
    }
}
