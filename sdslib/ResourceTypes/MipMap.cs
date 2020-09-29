using AutoMapper;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace sdslib.ResourceTypes
{
    public class Mipmap : Resource
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

        public new static Mipmap Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, uint? unknown32, uint? unknown32_2, byte[] rawData, IMapper mapper)
        {
            Mipmap texture = mapper.Map<Mipmap>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, unknown32, unknown32_2, rawData, null));
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
