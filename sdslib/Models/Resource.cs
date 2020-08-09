using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.Models
{
    public class Resource
    {
        public ResourceInfo Info { get; set; }

        public uint Size
        {
            get
            {
                return Constants.Resource.StandardHeaderSize + (uint)Data.Length;
            }
        }

        public ushort Version { get; set; }

        public uint SlotRamRequired { get; set; }

        public uint SlotVRamRequired { get; set; }

        public uint OtherRamRequired { get; set; }

        public uint OtherVRamRequired { get; set; }

        public uint Checksum
        {
            get
            {
                byte[] bytes = new byte[26];
                Array.Copy(BitConverter.GetBytes(Info.Type.Id), 0, bytes, 0, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Size), 0, bytes, 4, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Version), 0, bytes, 8, Constants.DataTypesSizes.UInt16);
                Array.Copy(BitConverter.GetBytes(SlotRamRequired), 0, bytes, 10, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(SlotVRamRequired), 0, bytes, 14, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(OtherRamRequired), 0, bytes, 18, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(OtherVRamRequired), 0, bytes, 22, Constants.DataTypesSizes.UInt32);
                return FNV.Hash32(bytes);
            }
        }

        public byte[] Data { get; set; }

        public virtual void Extract(string destination)
        {
            if (!Directory.Exists(Path.GetDirectoryName(destination)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
            }

            using (FileStream file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write))
            {
                file.Write(Data, 0, Data.Length);
            }
        }
    }
}
