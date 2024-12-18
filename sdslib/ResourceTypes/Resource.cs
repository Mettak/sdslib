using AutoMapper;
using Newtonsoft.Json;
using sdslib.Enums;
using System.Collections.Generic;
using System.IO;

namespace sdslib.ResourceTypes
{
    public class Resource : IResource
    {
        public const int StandardHeaderSizeV19 = 30;

        public const int StandardHeaderSizeV20 = 38;

        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        public ResourceInfo Info { get; private set; } = new ResourceInfo();

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
                if (ResourceNameHash.HasValue)
                {
                    return StandardHeaderSizeV20 + (uint)Serialize().Length;
                }

                return StandardHeaderSizeV19 + (uint)Serialize().Length;
            }
        }

        public ushort Version { get; set; }

        public uint SlotRamRequired { get; set; }

        public uint SlotVRamRequired { get; set; }

        public uint OtherRamRequired { get; set; }

        public uint OtherVRamRequired { get; set; }
         
        public ulong? ResourceNameHash { get; set; }

        [JsonIgnore]
        public uint Checksum
        {
            get
            {
                return FNV.Hash32(Info.Type.Id, Size, Version, ResourceNameHash ?? null, 
                    SlotRamRequired, SlotVRamRequired, OtherRamRequired, OtherVRamRequired);
            }
        }

        [JsonIgnore]
        public virtual byte[] Data { get; set; }

        [JsonIgnore]
        public virtual Dictionary<uint, List<ushort>> SupportedVersions { get; }

        public static Resource Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired,
            ulong? nameHash, byte[] rawData, IMapper mapper)
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
                ResourceNameHash = nameHash,
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
