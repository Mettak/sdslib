using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System;

namespace sdslib.ResourceTypes
{
    public class XML : Resource
    {
        public string TargetModule { get; set; }

        public byte Unknown8 { get; set; }

        public string Path { get; set; }

        public ushort Unknown16 { get; set; } = 1024;

        private readonly List<Node> _nodes = new List<Node>();

        public override byte[] Data
        {
            get
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.WriteUInt32((uint)_nodes.Count);

                    using (MemoryStream valuesDbStream = new MemoryStream())
                    {
                        using (MemoryStream xmlData = new MemoryStream())
                        {
                            foreach (var node in _nodes)
                            {
                                xmlData.WriteUInt32((uint)valuesDbStream.Position);
                                valuesDbStream.WriteUInt32(node.Name.Type);
                                valuesDbStream.WriteUInt32(node.Name.Unknown32);
                                valuesDbStream.WriteString(node.Name.Value.ToString());
                                valuesDbStream.WriteByte(0);

                                xmlData.WriteUInt32((uint)valuesDbStream.Position);
                                valuesDbStream.WriteUInt32(node.Value.Type);
                                valuesDbStream.WriteUInt32(node.Value.Unknown32);
                                valuesDbStream.WriteString(node.Value.Value.ToString());
                                valuesDbStream.WriteByte(0);

                                xmlData.WriteUInt32(node.Id);
                                xmlData.WriteUInt32((uint)node.Childs.Count);
                                foreach (var child in node.Childs)
                                {
                                    xmlData.WriteUInt32(child);
                                }

                                xmlData.WriteUInt32((uint)node.Attributes.Count);
                                foreach (var attribute in node.Attributes)
                                {
                                    xmlData.WriteUInt32((uint)valuesDbStream.Position);
                                    valuesDbStream.WriteUInt32(attribute.Name.Type);
                                    valuesDbStream.WriteUInt32(attribute.Name.Unknown32);
                                    valuesDbStream.WriteString(attribute.Name.Value.ToString());
                                    valuesDbStream.WriteByte(0);

                                    xmlData.WriteUInt32((uint)valuesDbStream.Position);
                                    valuesDbStream.WriteUInt32(attribute.Value.Type);
                                    valuesDbStream.WriteUInt32(attribute.Value.Unknown32);
                                    valuesDbStream.WriteString(attribute.Value.Value.ToString());
                                    valuesDbStream.WriteByte(0);
                                }
                            }

                            ms.WriteUInt32((uint)valuesDbStream.Length);
                            ms.Write(valuesDbStream.ReadAllBytes());
                            ms.Write(xmlData.ReadAllBytes());
                        }
                    }

                    return ms.ReadAllBytes();
                }
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        [JsonIgnore]
        public string XmlString
        {
            get
            {
                if (!_nodes.Any())
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
                    var root = _nodes.First(x => x.Id == 1);
                    xmlWriter.WriteStartElement(root.Name.Value.ToString());
                    foreach (var attribute in root.Attributes)
                    {
                        xmlWriter.WriteAttributeString(attribute.Name.Value.ToString(), attribute.Value.Value.ToString());
                    }

                    if (!string.IsNullOrEmpty(root.Value.Value.ToString()))
                    {
                        xmlWriter.WriteAttributeString($"__type", root.Value.Type.ToString());
                        xmlWriter.WriteAttributeString($"__unknown32", root.Value.Unknown32.ToString());
                        xmlWriter.WriteValue(root.Value.Value);
                    }

                    foreach (var childId in root.Childs)
                    {
                        var child = _nodes.First(x => x.Id == childId);
                        WriteXmlNode(xmlWriter, _nodes, child);
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
                writer.WriteAttributeString($"__unknown32", node.Value.Unknown32.ToString());
                writer.WriteValue(node.Value.Value);
            }

            foreach (var childId in node.Childs)
            {
                var child = _nodes.First(x => x.Id == childId);
                WriteXmlNode(writer, nodes, child);
            }

            writer.WriteEndElement();
        }

        public override byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteUInt32((uint)TargetModule.Length);
                ms.WriteString(TargetModule);
                ms.WriteUInt8(Unknown8);
                ms.WriteUInt32((uint)Path.Length);
                ms.WriteString(Path);
                ms.WriteUInt16(Unknown16);
                ms.Write(Data);
                return ms.ReadAllBytes();
            }
        }

        public new static XML Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, byte[] rawData)
        {
            XML xml = new XML
            {
                Info = resourceInfo,
                Version = version,
                SlotRamRequired = slotRamRequired,
                SlotVRamRequired = slotVRamRequired,
                OtherRamRequired = otherRamRequired,
                OtherVRamRequired = otherVRamRequired
            };

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

                        xml._nodes.Add(node);
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
