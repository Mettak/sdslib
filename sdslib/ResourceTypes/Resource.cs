using AutoMapper;
using Newtonsoft.Json;
using System.IO;

namespace sdslib.ResourceTypes
{
    public class Resource : IResource
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        public ResourceInfo Info { get; set; } = new ResourceInfo();

        [JsonIgnore]
        public string Name
        {
            get
            {
                if (Info.SourceDataDescription == "not available")
                {
                    return Guid;
                }

                return Info.SourceDataDescription;
            }
        }

        [JsonIgnore]
        public uint Size
        {
            get
            {
                if (Unknown32.HasValue && Unknown32_2.HasValue)
                {
                    return Constants.Resource.StandardHeaderSizeV20 + (uint)Serialize().Length;
                }

                return Constants.Resource.StandardHeaderSizeV19 + (uint)Serialize().Length;
            }
        }

        public ushort Version { get; set; }

        public uint SlotRamRequired { get; set; }

        public uint SlotVRamRequired { get; set; }

        public uint OtherRamRequired { get; set; }

        public uint OtherVRamRequired { get; set; }
         
        public uint? Unknown32 { get; set; }

        public uint? Unknown32_2 { get; set; }

        [JsonIgnore]
        public uint Checksum
        {
            get
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.WriteUInt32(Info.Type.Id);
                    ms.WriteUInt32(Size);
                    ms.WriteUInt16(Version);

                    if (Unknown32.HasValue)
                    {
                        ms.WriteUInt32(Unknown32.Value);
                    }

                    if (Unknown32_2.HasValue)
                    {
                        ms.WriteUInt32(Unknown32_2.Value);
                    }

                    ms.WriteUInt32(SlotRamRequired);
                    ms.WriteUInt32(SlotVRamRequired);
                    ms.WriteUInt32(OtherRamRequired);
                    ms.WriteUInt32(OtherVRamRequired);

                    return FNV.Hash32(ms.ReadAllBytes());
                }
            }
        }

        [JsonIgnore]
        public virtual byte[] Data { get; set; }

        public static Resource Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired,
            uint? unknown32, uint? unknown32_2, byte[] rawData, IMapper mapper)
        {
            return new Resource
            {
                Guid = System.Guid.NewGuid().ToString(),
                Info = resourceInfo,
                Version = version,
                SlotRamRequired = slotRamRequired,
                SlotVRamRequired = slotVRamRequired,
                OtherRamRequired = otherRamRequired,
                OtherVRamRequired = otherVRamRequired,
                Unknown32 = unknown32,
                Unknown32_2 = unknown32_2,
                Data = rawData
            };
        }

        public virtual byte[] Serialize()
        {
            return Data;
        }

        public virtual void Extract(string destination)
        {
            if (!Directory.Exists(Path.GetDirectoryName(destination)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
            }

            File.WriteAllBytes(destination, Data);
        }

        public virtual void ReplaceData(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            Data = File.ReadAllBytes(path);
        }
    }
}
