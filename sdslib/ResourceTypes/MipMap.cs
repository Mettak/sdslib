using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class MipMap : Resource
    {
        public override uint Size
        {
            get
            {
                return base.Size + (sizeof(UInt64) + sizeof(byte));
            }
        }

        public ulong NameHash
        {
            get
            {
                return FNV.Hash64(Encoding.UTF8.GetBytes(Info.SourceDataDescription));
            }
        }

        public byte Unknown8 { get; set; }

        public MipMap(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, byte[] rawData)
            : base(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, rawData)
        {
            Unknown8 = System.Convert.ToByte(BitConverter.ToBoolean(rawData, sizeof(UInt64)));
            Data = rawData.Skip(sizeof(UInt64) + sizeof(byte)).ToArray();
        }

        public override byte[] GetRawData()
        {
            byte[] bytes = new byte[Data.Length + sizeof(UInt64) + sizeof(byte)];
            Array.Copy(BitConverter.GetBytes(NameHash), 0, bytes, 0, sizeof(UInt64));
            Array.Copy(BitConverter.GetBytes(Unknown8), 0, bytes, 8, sizeof(byte));
            Array.Copy(Data, 0, bytes, 9, Data.Length);
            return bytes;
        }
    }
}
