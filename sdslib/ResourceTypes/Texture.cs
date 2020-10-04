using AutoMapper;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System;

namespace sdslib.ResourceTypes
{
    public class Texture : Resource
    {
        [JsonIgnore]
        public ulong NameHash
        {
            get
            {
                if (Version == 3)
                {
                    return ResourceNameHash ?? throw new NullReferenceException(nameof(ResourceNameHash));
                }
                
                return FNV.Hash64(Encoding.UTF8.GetBytes(Info.SourceDataDescription));
            }
        }

        public byte? Unknown8 { get; set; }

        public bool HasMipMap { get; set; }

        public new static Texture Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, ulong? nameHash, byte[] rawData, IMapper mapper)
        {
            Texture texture = mapper.Map<Texture>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, nameHash, rawData, null));
            using (MemoryStream memory = new MemoryStream(rawData))
            {
                if (version == 2)
                {
                    memory.Seek(sizeof(ulong), SeekOrigin.Begin);
                }

                else if (version == 3)
                {
                    memory.Seek(sizeof(ulong), SeekOrigin.Begin);
                    texture.Info.SourceDataDescription = $"{texture.ResourceNameHash}.dds";
                }

                if (version == 2)
                {
                    texture.Unknown8 = memory.ReadUInt8();
                }

                texture.HasMipMap = System.Convert.ToBoolean(memory.ReadUInt8());
                texture.Data = memory.ReadAllBytesFromCurrentPosition();
            }
            return texture;
        }

        public override byte[] Serialize()
        {
            using (MemoryStream memory = new MemoryStream())
            {
                if (Version == 2 && Unknown8.HasValue)
                {
                    memory.WriteUInt64(NameHash);
                    memory.WriteUInt8(Unknown8.Value);
                }

                else if (Version == 3 && ResourceNameHash.HasValue)
                {
                    memory.WriteUInt64(ResourceNameHash ?? 
                        throw new NullReferenceException(nameof(ResourceNameHash)));
                }

                memory.WriteUInt8(Convert.ToByte(HasMipMap));
                memory.Write(Data, 0, Data.Length);
                return memory.ReadAllBytes();
            }
        }
    }
}
