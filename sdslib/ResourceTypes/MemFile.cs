using AutoMapper;
using sdslib.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class MemFile : Resource
    {
        public string Path { get; set; }

        public override Dictionary<uint, List<ushort>> SupportedVersions =>
            new Dictionary<uint, List<ushort>>()
            {
                {
                    19U,
                    new List<ushort>()
                    {
                        2
                    }
                },
                {
                    20U,
                    new List<ushort>()
                    {
                        4
                    }
                },
            };

        public new static MemFile Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, ulong? nameHash, byte[] rawData, IMapper mapper)
        {
            MemFile type = mapper.Map<MemFile>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, nameHash, rawData, null));
            
            if (version == 2)
            {
                using (MemoryStream memory = new MemoryStream(rawData))
                {
                    uint pathLength = memory.ReadUInt32();
                    type.Path = memory.ReadString((int)pathLength);
                    memory.Seek(sizeof(uint), SeekOrigin.Current);
                    memory.Seek(sizeof(uint), SeekOrigin.Current);
                    byte[] buffer = memory.ReadAllBytesFromCurrentPosition();
                    type.Data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(buffer));
                }
            }

            else if(version == 4)
            {
                using (MemoryStream memory = new MemoryStream(rawData))
                {
                    memory.Seek(sizeof(uint), SeekOrigin.Begin);
                    uint pathLength = memory.ReadUInt32();
                    type.Path = memory.ReadString((int)pathLength);
                    type.Info.SourceDataDescription = type.Path;
                    memory.Seek(sizeof(uint), SeekOrigin.Current);
                    memory.Seek(sizeof(uint), SeekOrigin.Current);
                    uint dataLength = memory.ReadUInt32();
                    type.Data = memory.ReadAllBytesFromCurrentPosition();
                }
            }

            return type;
        }

        public override byte[] Serialize()
        {
            if (Version == 2)
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    byte[] unicodeData = Encoding.Unicode.GetBytes(Encoding.Unicode.GetString(Data));
                    memory.WriteUInt32((uint)Path.Length);
                    memory.WriteString(Path);
                    memory.WriteUInt32(1U);
                    memory.WriteUInt32((uint)unicodeData.Length);
                    memory.Write(unicodeData);
                    return memory.ReadAllBytes();
                }
            }

            else if (Version == 4)
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    memory.WriteUInt32(0U);
                    memory.WriteUInt32((uint)Path.Length);
                    memory.WriteString(Path);
                    memory.WriteUInt32(1U);
                    memory.WriteUInt32(16U);
                    memory.WriteUInt32((uint)Data.Length);
                    memory.Write(Data);
                    return memory.ReadAllBytes();
                }
            }

            else
            {
                return base.Serialize();
            }
        }
    }
}
