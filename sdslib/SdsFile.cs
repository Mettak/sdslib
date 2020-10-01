using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using sdslib.Enums;
using sdslib.ResourceTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using zlib;

namespace sdslib
{
    public class SdsFile : IDisposable
    {
        private const uint UEzl = 1819952469U;

        private const int MaxBlockSizeV19 = 16384;

        private const int MaxBlockSizeV20 = 65536;

        private readonly IMapper _mapper;

        public List<Resource> Resources { get; set; } = new List<Resource>();

        public SdsHeader Header { get; set; } = new SdsHeader();

        [JsonIgnore]
        public uint SlotRamRequired
        {
            get
            {
                return (uint)Resources.Sum(x => x.SlotRamRequired);
            }
        }

        [JsonIgnore]
        public uint SlotVRamRequired
        {
            get
            {
                return (uint)Resources.Sum(x => x.SlotVRamRequired);
            }
        }

        [JsonIgnore]
        public uint OtherRamRequired
        {
            get
            {
                return (uint)Resources.Sum(x => x.OtherRamRequired);
            }
        }

        [JsonIgnore]
        public uint OtherVRamRequired
        {
            get
            {
                return (uint)Resources.Sum(x => x.OtherVRamRequired);
            }
        }

        [JsonIgnore]
        public string XmlString
        {
            get
            {
                if (Header?.Version > 19)
                {
                    return null;
                }

                string xmlFile = "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\" ?>\n<xml>\n";

                foreach (var resource in Resources)
                {
                    xmlFile += "\t<ResourceInfo>\n\t\t<CustomDebugInfo/>\n\t\t";
                    xmlFile += "<TypeName>" + resource.Info.Type.ToString() + "</TypeName>\n\t\t";
                    xmlFile += "<SourceDataDescription>" + resource.Info.SourceDataDescription + "</SourceDataDescription>\n\t\t";
                    xmlFile += "<SlotRamRequired __type='Int'>" + resource.SlotRamRequired + "</SlotRamRequired>\n\t\t";
                    xmlFile += "<SlotVramRequired __type='Int'>" + resource.SlotVRamRequired + "</SlotVramRequired>\n\t\t";
                    xmlFile += "<OtherRamRequired __type='Int'>" + resource.OtherRamRequired + "</OtherRamRequired>\n\t\t";
                    xmlFile += "<OtherVramRequired __type='Int'>" + resource.OtherVRamRequired + "</OtherVramRequired>\n\t";
                    xmlFile += "</ResourceInfo>\n";
                }

                xmlFile += "</xml>\n";

                return xmlFile.Replace("\n", Environment.NewLine);
            }
        }

        public string Path { get; set; }

        public SdsFile()
        {
            _mapper = new MapperConfiguration(mc => mc.AddProfile(new MappingProfile())).CreateMapper();
        }

        public static SdsFile FromFile(string sdsPath)
        {
            SdsFile file = new SdsFile();
            file.Path = System.IO.Path.GetFullPath(sdsPath);
            file.Header = SdsHeader.FromFile(sdsPath);

            using (FileStream fileStream = new FileStream(sdsPath, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(file.Header.BlockTableOffset, SeekOrigin.Begin);

                #region Version 19

                if (file.Header.Version == 19U)
                {
                    if (fileStream.ReadUInt32() != UEzl)
                        throw new Exception("Invalid SDS file!");

                    fileStream.Seek(5, SeekOrigin.Current);

                    using (MemoryStream decompressedData = new MemoryStream())
                    {
                        while (true)
                        {
                            uint blockSize = fileStream.ReadUInt32();
                            EDataBlockType blockType = (EDataBlockType)fileStream.ReadUInt8();

                            if (blockSize == 0U)
                                break;

                            if (blockType == EDataBlockType.Compressed)
                            {
                                fileStream.Seek(16, SeekOrigin.Current);
                                uint compressedBlockSize = fileStream.ReadUInt32();

                                if (blockSize - 32U != compressedBlockSize)
                                    throw new Exception("Invalid block!");

                                fileStream.Seek(12, SeekOrigin.Current);
                                byte[] compressedBlock = new byte[compressedBlockSize];
                                fileStream.Read(compressedBlock, 0, compressedBlock.Length);

                                ZOutputStream decompressStream = new ZOutputStream(decompressedData);
                                decompressStream.Write(compressedBlock, 0, compressedBlock.Length);
                                decompressStream.finish();
                            }

                            else if (blockType == EDataBlockType.Uncompressed)
                            {
                                byte[] decompressedBlock = new byte[blockSize];
                                fileStream.Read(decompressedBlock, 0, decompressedBlock.Length);
                                decompressedData.Write(decompressedBlock, 0, decompressedBlock.Length);
                            }

                            else
                            {
                                throw new Exception("Invalid block type!");
                            }
                        }

                        decompressedData.SeekToStart();

                        fileStream.Seek(file.Header.XmlOffset, SeekOrigin.Begin);
                        byte[] xmlBytes = fileStream.ReadBytes((int)fileStream.Length - (int)fileStream.Position);
                        using (MemoryStream memoryStream = new MemoryStream(xmlBytes))
                        using (XmlReader xmlReader = XmlReader.Create(memoryStream))
                        {
                            ResourceInfo resourceInfo = new ResourceInfo();

                            List<Type> resourceTypes = new List<Type>();
                            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                            {
                                if (type.BaseType == typeof(Resource))
                                {
                                    resourceTypes.Add(type);
                                }
                            }

                            while (xmlReader.Read())
                            {
                                if (xmlReader.NodeType == XmlNodeType.Element)
                                {
                                    if (xmlReader.Name == "TypeName")
                                    {
                                        resourceInfo = new ResourceInfo();
                                        var resourceType = (EResourceType)Enum.Parse(typeof(EResourceType), xmlReader.ReadElementContentAsString());
                                        resourceInfo.Type = file.Header.ResourceTypes.First(x => x.Name == resourceType);
                                    }

                                    else if (xmlReader.Name == "SourceDataDescription")
                                    {
                                        resourceInfo.SourceDataDescription = xmlReader.ReadElementContentAsString();

                                        uint resId = decompressedData.ReadUInt32();
                                        if (resId != resourceInfo.Type.Id)
                                        {
                                            throw new InvalidDataException();
                                        }

                                        uint size = decompressedData.ReadUInt32();
                                        ushort version = decompressedData.ReadUInt16();
                                        uint slotRamRequired = decompressedData.ReadUInt32();
                                        uint slotVRamRequired = decompressedData.ReadUInt32();
                                        uint otherRamRequired = decompressedData.ReadUInt32();
                                        uint otherVRamRequired = decompressedData.ReadUInt32();
                                        uint checksum = decompressedData.ReadUInt32();
                                        byte[] rawData = decompressedData.ReadBytes((int)size - Resource.StandardHeaderSizeV19);

                                        var targetType = resourceTypes.First(x => x.Name == resourceInfo.Type.ToString());
                                        var deserializeMethod = targetType.GetMethod(nameof(Resource.Deserialize));
                                        var resourecInstance = (Resource)deserializeMethod.Invoke(null, new object[] {
                                        resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, null, null, rawData, file._mapper });
                                        file.Resources.Add(resourecInstance);
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion

                #region Version 20

                else if (file.Header.Version == 20U)
                {
                    if (fileStream.ReadUInt32() != UEzl)
                        throw new Exception("Invalid SDS file!");

                    fileStream.Seek(5, SeekOrigin.Current);

                    using (MemoryStream decompressedData = new MemoryStream())
                    {
                        while (true)
                        {
                            uint blockSize = fileStream.ReadUInt32();
                            EDataBlockType blockType = (EDataBlockType)fileStream.ReadUInt8();

                            if (blockSize == 0U)
                                break;

                            if (blockType == EDataBlockType.Compressed)
                            {
                                uint uncompressedSize = fileStream.ReadUInt32();
                                fileStream.Seek(12, SeekOrigin.Current);
                                uint compressedBlockSize = fileStream.ReadUInt32();

                                if (blockSize - 128U != compressedBlockSize)
                                {
                                    throw new Exception("Invalid block!");
                                }

                                fileStream.Seek(108, SeekOrigin.Current);
                                byte[] compressedBlock = new byte[compressedBlockSize];
                                fileStream.Read(compressedBlock, 0, compressedBlock.Length);
                                byte[] decompressed = Oodle.Decompress(compressedBlock, compressedBlock.Length, (int)uncompressedSize);
                                decompressedData.Write(decompressed);
                            }

                            else if (blockType == EDataBlockType.Uncompressed)
                            {
                                byte[] decompressedBlock = new byte[blockSize];
                                fileStream.Read(decompressedBlock, 0, decompressedBlock.Length);
                                decompressedData.Write(decompressedBlock, 0, decompressedBlock.Length);
                            }

                            else
                            {
                                throw new Exception("Invalid block type!");
                            }
                        }

                        List<Type> resourceTypes = new List<Type>();
                        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                        {
                            if (type.BaseType == typeof(Resource))
                            {
                                resourceTypes.Add(type);
                            }
                        }

                        decompressedData.SeekToStart();
                        while(decompressedData.Position != decompressedData.Length)
                        {
                            uint resId = decompressedData.ReadUInt32();

                            ResourceInfo resourceInfo = new ResourceInfo();
                            resourceInfo.Type = file.Header.ResourceTypes.First(x => x.Id == resId);

                            uint size = decompressedData.ReadUInt32();
                            ushort version = decompressedData.ReadUInt16();
                            uint unknown32 = decompressedData.ReadUInt32();
                            uint unknown32_2 = decompressedData.ReadUInt32();
                            uint slotRamRequired = decompressedData.ReadUInt32();
                            uint slotVRamRequired = decompressedData.ReadUInt32();
                            uint otherRamRequired = decompressedData.ReadUInt32();
                            uint otherVRamRequired = decompressedData.ReadUInt32();
                            uint checksum = decompressedData.ReadUInt32();
                            byte[] rawData = decompressedData.ReadBytes((int)size - Resource.StandardHeaderSizeV20);

                            var targetType = resourceTypes.First(x => x.Name == resourceInfo.Type.ToString());
                            var deserializeMethod = targetType.GetMethod(nameof(Resource.Deserialize));
                            var resourceInstance = (Resource)deserializeMethod.Invoke(null, new object[] {
                                        resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, unknown32, unknown32_2, rawData, file._mapper });
                            file.Resources.Add(resourceInstance);
                        }
                    }
                }

                #endregion
            }

            return file;
        }

        public void ExportToFile(string path)
        {
            using (FileStream sds = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                long currentPosition;
                long blockTableOffsetPosition;
                long xmlOffsetPosition;
                long headerChecksumPosition;

                sds.WriteString("SDS", sizeof(uint));
                sds.WriteUInt32(Header.Version);
                sds.WriteString(Header.Platform.ToString(), sizeof(uint));

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.WriteString("SDS", sizeof(uint));
                    ms.WriteUInt32(Header.Version);
                    ms.WriteString(Header.Platform.ToString(), sizeof(uint));
                    sds.WriteUInt32(FNV.Hash32(ms.ReadAllBytes()));
                }

                Header.ResourceTypeTableOffset = SdsHeader.HeaderSize;
                sds.WriteUInt32(Header.ResourceTypeTableOffset);
                blockTableOffsetPosition = sds.Position;
                sds.Seek(sizeof(uint), SeekOrigin.Current);
                xmlOffsetPosition = sds.Position;
                sds.Seek(sizeof(uint), SeekOrigin.Current);

                sds.WriteUInt32(SlotRamRequired);
                sds.WriteUInt32(SlotVRamRequired);
                sds.WriteUInt32(OtherRamRequired);
                sds.WriteUInt32(OtherVRamRequired);

                sds.WriteUInt32(SdsHeader.Unknown32_2C);
                sds.WriteUInt64((ulong)Header.GameVersion);

                sds.Seek(sizeof(ulong), SeekOrigin.Current);
                sds.WriteUInt32((uint)Resources.Count);

                headerChecksumPosition = sds.Position;
                sds.Seek(sizeof(uint), SeekOrigin.Current);

                sds.WriteUInt32((uint)Header.ResourceTypes.Count);
                foreach (ResourceType resourceType in Header.ResourceTypes)
                {
                    sds.WriteUInt32(resourceType.Id);
                    sds.WriteUInt32((uint)resourceType.ToString().Length);
                    sds.WriteString(resourceType.ToString());
                    sds.WriteUInt32(resourceType.Unknown32);
                }

                currentPosition = sds.Position;
                Header.BlockTableOffset = (uint)currentPosition;
                sds.Seek(blockTableOffsetPosition, SeekOrigin.Begin);
                sds.WriteUInt32((uint)currentPosition);
                sds.Seek(currentPosition, SeekOrigin.Begin);

                sds.WriteUInt32(UEzl);

                if (Header?.Version == 19)
                {
                    sds.WriteUInt32(MaxBlockSizeV19);
                }

                else if (Header?.Version == 20)
                {
                    sds.WriteUInt32(MaxBlockSizeV20);
                }

                sds.WriteUInt8(4);

                bool first = true;
                foreach (MemoryStream block in MergeDataIntoBlocks())
                {
                    block.SeekToStart();

                    if ((first || block.Length >= 10240) && Header?.Version != 20U)
                    {
                        byte[] blockData = block.ReadAllBytes();

                        #region Version 19

                        if (Header?.Version == 19)
                        {
                            MemoryStream compressedBlock = new MemoryStream();
                            ZOutputStream compressStream = new ZOutputStream(compressedBlock,
                                zlibConst.Z_BEST_COMPRESSION);
                            compressStream.Write(blockData, 0, blockData.Length);
                            compressStream.finish();

                            sds.WriteUInt32((uint)compressStream.TotalOut + 32U);
                            sds.WriteUInt8((byte)EDataBlockType.Compressed);
                            sds.WriteUInt32((uint)block.Length);

                            sds.WriteUInt32(32);
                            sds.WriteUInt32(81920);
                            sds.WriteUInt32(135200769);

                            sds.WriteUInt32((uint)compressStream.TotalOut);
                            sds.WriteUInt64(0);
                            sds.WriteUInt32(0);

                            compressedBlock.SeekToStart();
                            sds.Write(compressedBlock.ReadAllBytes());
                        }

                        #endregion

                        #region Version 20

                        //else if (Header?.Version == 20)
                        //{
                        //    byte[] compressed = Oodle.Compress(blockData, blockData.Length);
                        //    sds.WriteUInt32((uint)compressed.Length + 128U);
                        //    sds.WriteUInt8((byte)EDataBlockType.Compressed);
                        //    sds.WriteUInt32((uint)block.Length);

                        //    sds.WriteUInt32(128);
                        //    sds.WriteUInt32(65537);
                        //    sds.WriteUInt32((uint)block.Length);

                        //    sds.WriteUInt32((uint)compressed.Length);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt64(0);
                        //    sds.WriteUInt32(0);

                        //    sds.Write(compressed);
                        //}

                        #endregion
                    }

                    else
                    {
                        sds.WriteUInt32((uint)block.Length);
                        sds.WriteUInt8((byte)EDataBlockType.Uncompressed);
                        sds.Write(block.ReadAllBytes());
                    }

                    first = false;
                }

                sds.WriteUInt32(0);
                sds.WriteUInt8(0);

                if (Header?.Version == 20U)
                {
                    Header.XmlOffset = 0;
                    sds.Seek(xmlOffsetPosition, SeekOrigin.Begin);
                    sds.WriteUInt32(Header.XmlOffset);
                }

                else
                {
                    currentPosition = sds.Position;
                    Header.XmlOffset = (uint)currentPosition;
                    sds.Seek(xmlOffsetPosition, SeekOrigin.Begin);
                    sds.WriteUInt32((uint)currentPosition);
                }

                sds.Seek(headerChecksumPosition, SeekOrigin.Begin);

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.WriteUInt32(Header.ResourceTypeTableOffset);
                    ms.WriteUInt32(Header.BlockTableOffset);
                    ms.WriteUInt32(Header.XmlOffset);
                    ms.WriteUInt32(SlotRamRequired);
                    ms.WriteUInt32(SlotVRamRequired);
                    ms.WriteUInt32(OtherRamRequired);
                    ms.WriteUInt32(OtherVRamRequired);
                    ms.WriteUInt32(SdsHeader.Unknown32_2C);
                    ms.WriteUInt64((ulong)Header.GameVersion);
                    ms.WriteUInt64(0);
                    ms.WriteUInt32((uint)Resources.Count);
                    sds.WriteUInt32(FNV.Hash32(ms.ReadAllBytes()));
                }

                if (Header?.Version == 19U)
                {
                    sds.Seek(currentPosition, SeekOrigin.Begin);
                    sds.WriteString(XmlString);
                }
            }
        }

        public static SdsFile FromDirectory(string path)
        {
            if (!File.Exists($@"{path}\sdscontext.json"))
            {
                throw new InvalidDataException(@"""sdscontext.json"" missing");
            }

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Newtonsoft.Json.Formatting.Indented,
                Converters = { new StringEnumConverter() }
            };
            SdsFile file = JsonConvert.DeserializeObject<SdsFile>(File.ReadAllText($@"{path}\sdscontext.json"), settings);

            foreach (var resource in file.Resources)
            {
                resource.Info.Type = file.Header.ResourceTypes.First(x => x.ToString() == resource.GetType().Name);

                if (resource is Script)
                {
                    (resource as Script).Scripts.ForEach(x => x.Data = File.ReadAllBytes($@"{path}\{resource.Info.Type}\{resource.Name}\{x.Path}"));
                }

                else if (resource is XML)
                {
                    (resource as XML).ParseXml($@"{path}\{resource.Info.Type}\{resource.Name}.xml");
                }

                else
                {
                    resource.Data = File.ReadAllBytes($@"{path}\{resource.Info.Type}\{resource.Name}");
                }
            }

            return file;
        }

        public void ExportToDirectory(string path)
        {
            Resources.ForEach(x => x.Extract($@"{path}\{System.IO.Path.GetFileNameWithoutExtension(Header.Name)}\{x.Info.Type}\{x.Name}"));
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Newtonsoft.Json.Formatting.Indented,
                Converters = { new StringEnumConverter() }
            };
            File.WriteAllText($@"{path}\{System.IO.Path.GetFileNameWithoutExtension(Header.Name)}\sdscontext.json",
                JsonConvert.SerializeObject(this, settings));
        }

        internal List<MemoryStream> MergeDataIntoBlocks()
        {
            MemoryStream mergedData = new MemoryStream();
            foreach (var resource in Resources)
            {
                mergedData.WriteUInt32(resource.Info.Type.Id);
                mergedData.WriteUInt32(resource.Size);
                mergedData.WriteUInt16(resource.Version);
                mergedData.WriteUInt32(resource.SlotRamRequired);
                mergedData.WriteUInt32(resource.SlotVRamRequired);
                mergedData.WriteUInt32(resource.OtherRamRequired);
                mergedData.WriteUInt32(resource.OtherVRamRequired);

                if (resource.Unknown32.HasValue &&
                    resource.Unknown32_2.HasValue)
                {
                    mergedData.WriteUInt32(resource.Unknown32.Value);
                    mergedData.WriteUInt32(resource.Unknown32_2.Value);
                }

                mergedData.WriteUInt32(resource.Checksum);
                mergedData.Write(resource.Serialize());
            }

            mergedData.SeekToStart();

            int numberOfBlocks = (int)mergedData.Length;
            if (Header?.Version == 19U)
            {
                numberOfBlocks /= MaxBlockSizeV19;
            }

            else if (Header?.Version == 20U)
            {
                numberOfBlocks /= MaxBlockSizeV20;
            }

            List<MemoryStream> dataBlocks = new List<MemoryStream>();
            for (int i = 0; i < numberOfBlocks; i++)
            {
                MemoryStream dataBlock = new MemoryStream();

                if (Header?.Version == 19U)
                {
                    dataBlock.Write(mergedData.ReadBytes(MaxBlockSizeV19));
                }

                else if (Header?.Version == 20U)
                {
                    dataBlock.Write(mergedData.ReadBytes(MaxBlockSizeV20));
                }

                dataBlocks.Add(dataBlock);
            }

            if (mergedData.Position != mergedData.Length)
            {
                MemoryStream dataBlock = new MemoryStream();
                dataBlock.Write(mergedData.ReadBytes((int)mergedData.Length - (int)mergedData.Position));
                dataBlocks.Add(dataBlock);
            }

            mergedData.Close();
            return dataBlocks;
        }

        public void AddResource<T>(T resource) where T : Resource
        {
            // Later will be replaced with an abstract class
            if (typeof(T).BaseType != typeof(Resource))
            {
                throw new Exception("Cannot add base type with this function.");
            }

            string typeName = resource.GetType().Name;

            resource.Info.Type = Header.ResourceTypes.First(x => x.ToString() == typeName);
            Resources.Add(resource);
        }

        public T GetResourceByTypeAndName<T>(string name) where T : Resource
        {
            return (T)Resources.FirstOrDefault(x => x is T && x.Name == name);
        }

        public void ExtractResourcesByType<T>(string path) where T : Resource
        {
            // Later will be replaced with an abstract class
            if (typeof(T).BaseType != typeof(Resource))
            {
                throw new Exception("Cannot call this function with base type.");
            }

            Resources.FindAll(x => x is T).ForEach(x => x.Extract($@"{path}\{x.Name}"));
        }

        public void AddResourceType(EResourceType resourceType)
        {
            if (Header.ResourceTypes.Any(x => x.Name == resourceType))
            {
                throw new Exception("SDS file already contains this resource type");
            }

            uint index = 0;
            if (Header.ResourceTypes.Any())
            {
                index = Header.ResourceTypes.Last().Id + 1;
            }

            Header.ResourceTypes.Add(new ResourceType
            {
                Id = index,
                Name = resourceType
            });
        }

        public void Dispose()
        {
            Header = null;
            Resources = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
