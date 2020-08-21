using AutoMapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class MipMap : Resource
    {
        [JsonIgnore]
        public ulong NameHash
        {
            get
            {
                return FNV.Hash64(Encoding.UTF8.GetBytes(Info.SourceDataDescription));
            }
        }

        [JsonIgnore]
        public byte Unknown8 { get; set; } = 0;

        public new static MipMap Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, byte[] rawData, IMapper mapper)
        {
            MipMap texture = Global.Mapper.Map<MipMap>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, rawData, null));
            using (MemoryStream memory = new MemoryStream(rawData))
            {
                memory.Seek(sizeof(ulong), SeekOrigin.Begin);
                texture.Unknown8 = memory.ReadUInt8();
                texture.Data = memory.ReadAllBytesFromCurrentPosition();
            }
            return texture;
        }

        public override byte[] Serialize()
        {
            using (MemoryStream memory = new MemoryStream())
            {
                memory.WriteUInt64(NameHash);
                memory.WriteUInt8(Unknown8);
                memory.Write(Data, 0, Data.Length);
                return memory.ReadAllBytes();
            }
        }
    }
}
