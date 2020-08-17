using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class XML : Resource
    {
        public string TargetModule { get; set; }

        public byte Unknown8 { get; set; }

        public string Path { get; set; }

        public ushort Unknown16 { get; set; } = 1024;

        public List<Node> Nodes { get; set; }

        public new static XML Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, byte[] rawData)
        {
            XML xml = Global.Mapper.Map<XML>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, rawData));
            using (MemoryStream memory = new MemoryStream(rawData))
            {
                xml.TargetModule = memory.ReadString((int)memory.ReadUInt32());
                xml.Unknown8 = memory.ReadUInt8();
                xml.Path = memory.ReadString((int)memory.ReadUInt32());

                if (!xml.Path.Contains("jukebox_music"))
                {
                    return null;
                }

                xml.Unknown16 = memory.ReadUInt16();
                uint nodesCount = memory.ReadUInt32();
                byte[] valuesDb = new byte[memory.ReadUInt32()];

                memory.Read(valuesDb, 0, valuesDb.Length);
                using (MemoryStream valuesDbStream = new MemoryStream(valuesDb))
                {
                    for (int i = 0; i < nodesCount; i++)
                    {


                        uint nameOffset = memory.ReadUInt32();
                        valuesDbStream.Seek(nameOffset, SeekOrigin.Begin);
                        uint nameType = valuesDbStream.ReadUInt32();
                        uint nameUnknown32 = valuesDbStream.ReadUInt32();
                        string nameValue = valuesDbStream.ReadStringDynamic();

                        uint valueOffset = memory.ReadUInt32();
                        valuesDbStream.Seek(valueOffset, SeekOrigin.Begin);
                        uint valueType = valuesDbStream.ReadUInt32();
                        uint valueUnknown32 = valuesDbStream.ReadUInt32();
                        string valueValue = valuesDbStream.ReadStringDynamic();

                        uint id = memory.ReadUInt32();
                        uint childCount = memory.ReadUInt32();
                        List<uint> childIds = new List<uint>();
                        for (int j = 0; j < childCount; j++)
                        {
                            childIds.Add(memory.ReadUInt32());
                        }

                        uint attributesCount = memory.ReadUInt32();
                        for (int k = 0; k < attributesCount; k++)
                        {
                            memory.ReadUInt32();
                            memory.ReadUInt32();
                        }
                    }
                }
            }
            return xml;
        }
    }

    public class Node
    {

    }
}
