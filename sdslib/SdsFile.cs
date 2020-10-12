using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using sdslib.Enums;
using sdslib.ResourceTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using zlib;

namespace sdslib
{
    public partial class SdsFile : IDisposable
    {
        private const uint UEzl = 1819952469U;

        private const int MaxBlockSizeV19 = 16384;

        private const int MaxBlockSizeV20 = 65536;

        private readonly IMapper _mapper;

        [JsonIgnore]
        public ReadOnlyCollection<ResourceType> ResourceTypes
        {
            get
            {
                return _resourceTypes.AsReadOnly();
            }
        }

        [JsonProperty("ResourceTypes")]
        private List<ResourceType> _resourceTypes = new List<ResourceType>();

        [JsonIgnore]
        public ReadOnlyCollection<Resource> Resources
        {
            get
            {
                return _resources.AsReadOnly();
            }
        }

        [JsonProperty("Resources")]
        private List<Resource> _resources = new List<Resource>();

        public SdsHeader Header { get; private set; } = new SdsHeader();

        [JsonIgnore]
        public uint SlotRamRequired
        {
            get
            {
                return (uint)_resources.Sum(x => x.SlotRamRequired);
            }
        }

        [JsonIgnore]
        public uint SlotVRamRequired
        {
            get
            {
                return (uint)_resources.Sum(x => x.SlotVRamRequired);
            }
        }

        [JsonIgnore]
        public uint OtherRamRequired
        {
            get
            {
                return (uint)_resources.Sum(x => x.OtherRamRequired);
            }
        }

        [JsonIgnore]
        public uint OtherVRamRequired
        {
            get
            {
                return (uint)_resources.Sum(x => x.OtherVRamRequired);
            }
        }

        /// <summary>
        /// Xml used in version 19 at the end of the file.
        /// Contains resources with some basic info.
        /// Newer versions returns null.
        /// </summary>
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

                foreach (var resource in _resources)
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
                fileStream.Seek(file.Header.ResourceTypeTableOffset, SeekOrigin.Begin);
                uint numberOfResources = fileStream.ReadUInt32();
                for (int i = 0; i < numberOfResources; i++)
                {
                    ResourceType resourceType = new ResourceType();
                    resourceType.Id = fileStream.ReadUInt32();
                    uint resourceLenght = fileStream.ReadUInt32();
                    string typeStr = fileStream.ReadString((int)resourceLenght).Replace(" ", "");
                    resourceType.Name = (EResourceType)Enum.Parse(typeof(EResourceType), typeStr);
                    uint unknown32 = fileStream.ReadUInt32();
                    if (unknown32 != resourceType.Unknown32)
                    {
                        throw new InvalidDataException(unknown32.ToString());
                    }
                    file.AddResourceType(resourceType);
                }

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
                                        resourceInfo.Type = file._resourceTypes.First(x => x.Name == resourceType);
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
                                        resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, null, rawData, file._mapper });
                                        file._resources.Add(resourecInstance);
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
                            resourceInfo.Type = file._resourceTypes.First(x => x.Id == resId);

                            uint size = decompressedData.ReadUInt32();
                            ushort version = decompressedData.ReadUInt16();
                            ulong nameHash = decompressedData.ReadUInt64();
                            uint slotRamRequired = decompressedData.ReadUInt32();
                            uint slotVRamRequired = decompressedData.ReadUInt32();
                            uint otherRamRequired = decompressedData.ReadUInt32();
                            uint otherVRamRequired = decompressedData.ReadUInt32();
                            uint checksum = decompressedData.ReadUInt32();
                            byte[] rawData = decompressedData.ReadBytes((int)size - Resource.StandardHeaderSizeV20);

                            var targetType = resourceTypes.First(x => x.Name == resourceInfo.Type.ToString());
                            var deserializeMethod = targetType.GetMethod(nameof(Resource.Deserialize));
                            var resourceInstance = (Resource)deserializeMethod.Invoke(null, new object[] {
                                        resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, nameHash, rawData, file._mapper });
                            file._resources.Add(resourceInstance);
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
                sds.WriteUInt32((uint)_resources.Count);

                headerChecksumPosition = sds.Position;
                sds.Seek(sizeof(uint), SeekOrigin.Current);

                sds.WriteUInt32((uint)_resourceTypes.Count);
                foreach (ResourceType resourceType in _resourceTypes)
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
                int blockSize = Header?.Version == 19U ? MaxBlockSizeV19 : MaxBlockSizeV20;
                foreach (MemoryStream block in MergeDataIntoBlocks(blockSize))
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

                sds.WriteUInt32(FNV.Hash32(Header.ResourceTypeTableOffset, Header.BlockTableOffset, Header.XmlOffset, SlotRamRequired, SlotVRamRequired, 
                    OtherRamRequired, OtherVRamRequired, SdsHeader.Unknown32_2C, (ulong)Header.GameVersion, 0UL, (uint)_resources.Count));

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

            foreach (var resource in file._resources)
            {
                resource.Info.Type = file._resourceTypes.First(x => x.ToString() == resource.GetType().Name);

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
            _resources.ForEach(x => x.Extract($@"{path}\{System.IO.Path.GetFileNameWithoutExtension(Header.Name)}\{x.Info.Type}\{x.Name}"));
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Newtonsoft.Json.Formatting.Indented,
                Converters = { new StringEnumConverter() }
            };
            File.WriteAllText($@"{path}\{System.IO.Path.GetFileNameWithoutExtension(Header.Name)}\sdscontext.json",
                JsonConvert.SerializeObject(this, settings));
        }

        public T GetResourceByTypeAndName<T>(string name) where T : Resource
        {
            return (T)_resources.FirstOrDefault(x => x is T && x.Name == name);
        }

        public void ExtractResourcesByType<T>(string path) where T : Resource
        {
            // Later will be replaced with an abstract class
            if (typeof(T).BaseType != typeof(Resource))
            {
                throw new Exception("Cannot call this function with base type.");
            }

            _resources.FindAll(x => x is T).ForEach(x => x.Extract($@"{path}\{x.Name}"));
        }

        public void AddResource(Resource resource)
        {
            if (_resourceTypes == null)
            {
                throw new ArgumentNullException(nameof(_resourceTypes));
            }

            if (resource.GetType().BaseType != typeof(Resource))
            {
                throw new Exception("Cannot add base type with this function.");
            }

            string typeName = resource.GetType().Name;

            var type = _resourceTypes.FirstOrDefault(x => x.ToString() == typeName);
            resource.Info.Type = type ?? throw new Exception($"Resource type {typeName} not found");
            _resources.Add(resource);
        }

        public void AddResourceType(ResourceType item)
        {
            if (_resourceTypes.Any(x => x.Name == item.Name))
            {
                throw new Exception("Collection already contains this resource type");
            }

            if (_resourceTypes.Any(x => x.Id == item.Id))
            {
                throw new Exception("Index already used by another resource type");
            }

            _resourceTypes.Add(item);
        }

        public void AddResourceType(EResourceType item)
        {
            if (_resourceTypes.Any(x => x.Name == item))
            {
                throw new Exception("Collection already contains this resource type");
            }

            uint index = 0;
            if (_resourceTypes.Count > 0)
            {
                index = _resourceTypes.Last().Id + 1;
            }

            _resourceTypes.Add(new ResourceType
            {
                Id = index,
                Name = item
            });
        }

        internal List<MemoryStream> MergeDataIntoBlocks(int blockSize)
        {
            MemoryStream mergedData = new MemoryStream();
            foreach (var resource in _resources)
            {
                mergedData.WriteUInt32(resource.Info.Type.Id);
                mergedData.WriteUInt32(resource.Size);
                mergedData.WriteUInt16(resource.Version);

                if (resource.ResourceNameHash.HasValue)
                {
                    mergedData.WriteUInt64(resource.ResourceNameHash ??
                        throw new NullReferenceException(nameof(resource.ResourceNameHash)));
                }

                mergedData.WriteUInt32(resource.SlotRamRequired);
                mergedData.WriteUInt32(resource.SlotVRamRequired);
                mergedData.WriteUInt32(resource.OtherRamRequired);
                mergedData.WriteUInt32(resource.OtherVRamRequired);
                mergedData.WriteUInt32(resource.Checksum);
                mergedData.Write(resource.Serialize());
            }

            mergedData.SeekToStart();

            int numberOfBlocks = (int)mergedData.Length / blockSize;

            List<MemoryStream> dataBlocks = new List<MemoryStream>();
            for (int i = 0; i < numberOfBlocks; i++)
            {
                MemoryStream dataBlock = new MemoryStream();
                dataBlock.Write(mergedData.ReadBytes(blockSize));
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

        public void Dispose()
        {
            Header = null;
            _resources = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
