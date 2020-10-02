using AutoMapper;
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

        public new static MemFile Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, uint? unknown32, uint? unknown32_2, byte[] rawData, IMapper mapper)
        {
            MemFile type = mapper.Map<MemFile>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, unknown32, unknown32_2, rawData, null));
            
            if (version == 4)
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
            if (Version != 4)
            {
                return base.Serialize();
            }

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
    }
}
