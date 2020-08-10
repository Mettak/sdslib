using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class Texture : Resource
    {
        public override uint Size
        {
            get
            {
                return base.Size + (sizeof(UInt64) + sizeof(UInt16));
            }
        }

        public ulong NameHash
        {
            get
            {
                return FNV.Hash64(Encoding.UTF8.GetBytes(Info.SourceDataDescription));
            }
        }

        public byte Unknown8 { get; set; } = 0;

        public bool HasMipMap { get; set; }

        public Texture(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, byte[] rawData) : 
            base(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, rawData)
        {
            Unknown8 = System.Convert.ToByte(BitConverter.ToBoolean(rawData, sizeof(UInt64)));
            HasMipMap = BitConverter.ToBoolean(rawData, sizeof(UInt64) + sizeof(byte));
            Data = rawData.Skip(sizeof(UInt64) + sizeof(UInt16)).ToArray();
        }

        public override byte[] GetRawData()
        {
            byte[] bytes = new byte[Data.Length + sizeof(UInt64) + sizeof(UInt16)];
            Array.Copy(BitConverter.GetBytes(NameHash), 0, bytes, 0, sizeof(UInt64));
            Array.Copy(BitConverter.GetBytes(Unknown8), 0, bytes, 8, sizeof(byte));
            Array.Copy(BitConverter.GetBytes(HasMipMap), 0, bytes, 9, sizeof(byte));
            Array.Copy(Data, 0, bytes, 10, Data.Length);
            return bytes;
        }
    }
}
