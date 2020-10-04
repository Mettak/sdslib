using AutoMapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class Flash : Resource
    {
        public string Path { get; set; }

        public ulong Unknown64 { get; set; }

        public string NameWithoutExtension { get; set; }

        public new static Flash Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, ulong? nameHash, byte[] rawData, IMapper mapper)
        {
            Flash type = mapper.Map<Flash>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, nameHash, rawData, null));
            
            using (MemoryStream memory = new MemoryStream(rawData))
            {
                ushort pathLength = memory.ReadUInt16();
                type.Path = memory.ReadString(pathLength);
                type.Info.SourceDataDescription = type.Path;
                type.Unknown64 = memory.ReadUInt64();
                pathLength = memory.ReadUInt16();
                type.NameWithoutExtension = memory.ReadString(pathLength);
                uint dataLength = memory.ReadUInt32();
                type.Data = memory.ReadAllBytesFromCurrentPosition();
            }

            return type;
        }

        public override byte[] Serialize()
        {
            using (MemoryStream memory = new MemoryStream())
            {
                memory.WriteUInt16((ushort)Path.Length);
                memory.WriteString(Path);
                memory.WriteUInt64(Unknown64);
                memory.WriteUInt16((ushort)NameWithoutExtension.Length);
                memory.WriteString(NameWithoutExtension);
                memory.WriteUInt32((uint)Data.Length);
                memory.Write(Data);
                return memory.ReadAllBytes();
            }
        }

        public void ExtractAsSwf(string destination)
        {
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(destination)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
            }

            if (System.IO.Path.GetExtension(Path).ToLower() == ".swf")
            {
                base.Extract(destination);
                return;
            }

            using(MemoryStream ms = new MemoryStream(Data))
            {
                ms.SeekToStart();
                ms.WriteString("FWS");
                File.WriteAllBytes(destination, ms.ReadAllBytes());
            }
        }
    }
}
