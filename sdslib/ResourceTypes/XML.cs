using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace sdslib.ResourceTypes
{
    public class XML : Resource
    {
        public string TargetModule { get; set; }

        public byte Unknown8 { get; set; }

        public string Path { get; set; }

        public ushort Unknown16 { get; set; } = 1024;

        [JsonIgnore]
        public List<Node> Nodes { get; set; } = new List<Node>();

        [JsonIgnore]
        public string XmlString
        {
            get
            {
                if (!Nodes.Any())
                {
                    return string.Empty;
                }

                StringBuilder stringBuilder = new StringBuilder();

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = Encoding.UTF8;
                using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings))
                {
                    xmlWriter.WriteStartDocument();
                    var root = Nodes.First(x => x.Id == 1);
                    xmlWriter.WriteStartElement(root.Name.Value.ToString());
                    foreach (var attribute in root.Attributes)
                    {
                        xmlWriter.WriteAttributeString(attribute.Name.Value.ToString(), attribute.Value.Value.ToString());
                    }

                    if (!string.IsNullOrEmpty(root.Value.Value.ToString()))
                    {
                        xmlWriter.WriteAttributeString($"__type", root.Value.Type.ToString());
                        xmlWriter.WriteValue(root.Value.Value);
                    }

                    foreach (var childId in root.Childs)
                    {
                        var child = Nodes.First(x => x.Id == childId);
                        WriteXmlNode(xmlWriter, Nodes, child);
                    }

                    xmlWriter.WriteEndElement();
                }

                return stringBuilder.ToString();
            }
        }

        internal void WriteXmlNode(XmlWriter writer, List<Node> nodes, Node node)
        {
            writer.WriteStartElement(node.Name.Value.ToString());

            foreach (var attribute in node.Attributes)
            {
                writer.WriteAttributeString(attribute.Name.Value.ToString(), attribute.Value.Value.ToString());
            }

            if (!string.IsNullOrEmpty(node.Value.Value.ToString()))
            {
                writer.WriteAttributeString($"__type", node.Value.Type.ToString());
                writer.WriteValue(node.Value.Value);
            }

            foreach (var childId in node.Childs)
            {
                var child = Nodes.First(x => x.Id == childId);
                WriteXmlNode(writer, nodes, child);
            }

            writer.WriteEndElement();
        }

        public new static XML Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, byte[] rawData)
        {
            XML xml = Global.Mapper.Map<XML>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, rawData));
            using (MemoryStream memory = new MemoryStream(rawData))
            {
                xml.TargetModule = memory.ReadString((int)memory.ReadUInt32());
                xml.Unknown8 = memory.ReadUInt8();
                xml.Path = memory.ReadString((int)memory.ReadUInt32());

                xml.Unknown16 = memory.ReadUInt16();

                if (xml.Unknown16 != 1024)
                {
                    return xml;
                }

                uint nodesCount = memory.ReadUInt32();
                byte[] valuesDb = new byte[memory.ReadUInt32()];

                memory.Read(valuesDb, 0, valuesDb.Length);
                using (MemoryStream valuesDbStream = new MemoryStream(valuesDb))
                {
                    for (int i = 0; i < nodesCount; i++)
                    {
                        Node node = new Node();

                        uint nameOffset = memory.ReadUInt32();
                        valuesDbStream.Seek(nameOffset, SeekOrigin.Begin);
                        node.Name.Type = valuesDbStream.ReadUInt32();
                        node.Name.Unknown32 = valuesDbStream.ReadUInt32();
                        node.Name.Value = valuesDbStream.ReadStringDynamic();

                        uint valueOffset = memory.ReadUInt32();
                        valuesDbStream.Seek(valueOffset, SeekOrigin.Begin);
                        node.Value.Type = valuesDbStream.ReadUInt32();
                        node.Value.Unknown32 = valuesDbStream.ReadUInt32();
                        node.Value.Value = valuesDbStream.ReadStringDynamic();

                        node.Id = memory.ReadUInt32();

                        uint childCount = memory.ReadUInt32();
                        for (int j = 0; j < childCount; j++)
                        {
                            node.Childs.Add(memory.ReadUInt32());
                        }

                        uint attributesCount = memory.ReadUInt32();
                        for (int k = 0; k < attributesCount; k++)
                        {
                            Node.Attribute attribute = new Node.Attribute();

                            uint attributeNameOffset = memory.ReadUInt32();
                            valuesDbStream.Seek(attributeNameOffset, SeekOrigin.Begin);
                            attribute.Name.Type = valuesDbStream.ReadUInt32();
                            attribute.Name.Unknown32 = valuesDbStream.ReadUInt32();
                            attribute.Name.Value = valuesDbStream.ReadStringDynamic();

                            uint attributeValueOffset = memory.ReadUInt32();
                            valuesDbStream.Seek(attributeValueOffset, SeekOrigin.Begin);
                            attribute.Value.Type = valuesDbStream.ReadUInt32();
                            attribute.Value.Unknown32 = valuesDbStream.ReadUInt32();
                            attribute.Value.Value = valuesDbStream.ReadStringDynamic();

                            node.Attributes.Add(attribute);
                        }

                        xml.Nodes.Add(node);
                    }
                }
            }
            return xml;
        }

        public void ParseXml(string path)
        {

        }

        public override void Extract(string destination)
        {
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(destination)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
            }

            if (Unknown16 == 1024)
            {
                System.IO.File.WriteAllText(destination, XmlString);
            }

            else
            {
                System.IO.File.WriteAllBytes(destination, Data);
            }
        }

        public class Node
        {
            public class Property
            {
                public uint Type { get; set; } = 4;

                public uint Unknown32 { get; set; } = 0;

                public object Value { get; set; }
            }

            public class Attribute
            {
                public Property Name { get; set; } = new Property();

                public Property Value { get; set; } = new Property();
            }

            public uint Id { get; set; }

            public Property Name { get; set; } = new Property();

            public Property Value { get; set; } = new Property();

            public List<uint> Childs { get; set; } = new List<uint>();

            public List<Attribute> Attributes { get; set; } = new List<Attribute>();
        }
    }
}
