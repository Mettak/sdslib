using Newtonsoft.Json;
using sdslib.Enums;
using sdslib.ResourceTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using zlib;

namespace sdslib
{
    /// <summary>
    /// Provides functions for extracting or replacing files in SDS file (version 19).
    /// </summary>
    public class SdsFile
    {
        public List<Resource> Resources { get; set; } = new List<Resource>();

        public SdsHeader Header { get; set; }

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
                string xmlFile = "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\" ?>\n<xml>\n";

                foreach (var resource in Resources)
                {
                    xmlFile += "\t<ResourceInfo>\n\t\t<CustomDebugInfo/>\n\t\t";
                    xmlFile += "<TypeName>" + resource.Info.Type.DisplayName + "</TypeName>\n\t\t";
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

        public SdsFile(string sdsPath)
        {
            Header = new SdsHeader(sdsPath);

            MemoryStream decompressedData;
            using (FileStream fileStream = new FileStream(sdsPath, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(Header.BlockTableOffset, SeekOrigin.Begin);

                if (fileStream.ReadUInt32() != 1819952469U)
                    throw new Exception("Invalid SDS file!");

                fileStream.Seek(5, SeekOrigin.Current);

                decompressedData = new MemoryStream();
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

                fileStream.Seek(Header.XmlOffset, SeekOrigin.Begin);
                byte[] xmlBytes = fileStream.ReadBytes((int)fileStream.Length - (int)fileStream.Position);
                using (MemoryStream memoryStream = new MemoryStream(xmlBytes))
                using (XmlReader xmlReader = XmlReader.Create(memoryStream))
                {
                    ResourceInfo resourceInfo = new ResourceInfo();
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            if (xmlReader.Name == "TypeName")
                            {
                                resourceInfo = new ResourceInfo();
                                var resourceType = (EResourceType)Enum.Parse(typeof(EResourceType), xmlReader.ReadElementContentAsString());
                                resourceInfo.Type = Header.ResourceTypes.First(x => x.Name == resourceType);
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
                                byte[] rawData = decompressedData.ReadBytes((int)size - Constants.Resource.StandardHeaderSize);

                                switch(resourceInfo.Type.Name)
                                {
                                    case EResourceType.Texture:
                                        Resources.Add(new ResourceTypes.Texture(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, rawData));
                                        break;

                                    case EResourceType.Mipmap:
                                        Resources.Add(new ResourceTypes.MipMap(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, rawData));
                                        break;

                                    default:
                                        Resources.Add(new Resource(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, rawData));
                                        break;
                                }
                            }
                        }
                    }
                }

                decompressedData.Close();
            }
        }

        public void ExportToFile(string path)
        {
            using (FileStream sds = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                long currentPosition;
                long blockTableOffsetPosition;
                long xmlOffsetPosition;
                long headerChecksumPosition;

                sds.WriteString("SDS", Constants.DataTypesSizes.UInt32);
                sds.WriteUInt32(Header.Version);
                sds.WriteString(Header.Platform.ToString(), Constants.DataTypesSizes.UInt32);
                byte[] hash1 = new byte[3 * Constants.DataTypesSizes.UInt32];
                Array.Copy(Encoding.UTF8.GetBytes("SDS\0"), 0, hash1, 0, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Header.Version), 0, hash1, 4, Constants.DataTypesSizes.UInt32);
                Array.Copy(Encoding.UTF8.GetBytes(Header.Platform.ToString()), 0, hash1, 8, Header.Platform.ToString().Length);
                sds.WriteUInt32(FNV.Hash32(hash1));

                Header.ResourceTypeTableOffset = 72;
                sds.WriteUInt32(Header.ResourceTypeTableOffset);
                blockTableOffsetPosition = sds.Position;
                sds.Seek(Constants.DataTypesSizes.UInt32, SeekOrigin.Current);
                xmlOffsetPosition = sds.Position;
                sds.Seek(Constants.DataTypesSizes.UInt32, SeekOrigin.Current);

                sds.WriteUInt32(SlotRamRequired);
                sds.WriteUInt32(SlotVRamRequired);
                sds.WriteUInt32(OtherRamRequired);
                sds.WriteUInt32(OtherVRamRequired);

                sds.WriteUInt32(Constants.SdsHeader.Unknown32_2C);
                sds.WriteUInt64((ulong)Header.GameVersion);

                sds.Seek(Constants.DataTypesSizes.UInt64, SeekOrigin.Current);
                sds.WriteUInt32((uint)Resources.Count);

                headerChecksumPosition = sds.Position;
                sds.Seek(Constants.DataTypesSizes.UInt32, SeekOrigin.Current);

                sds.WriteUInt32((uint)Header.ResourceTypes.Count);
                foreach (ResourceType resourceType in Header.ResourceTypes)
                {
                    sds.WriteUInt32(resourceType.Id);
                    sds.WriteUInt32((uint)resourceType.DisplayName.Length);
                    sds.WriteString(resourceType.DisplayName);
                    sds.WriteUInt32(resourceType.Unknown32);
                }

                currentPosition = sds.Position;
                Header.BlockTableOffset = (uint)currentPosition;
                sds.Seek(blockTableOffsetPosition, SeekOrigin.Begin);
                sds.WriteUInt32((uint)currentPosition);
                sds.Seek(currentPosition, SeekOrigin.Begin);

                sds.WriteUInt32(1819952469U);
                sds.WriteUInt32(Constants.SdsHeader.BlockSize);
                sds.WriteUInt8(4);

                bool first = true;
                foreach (MemoryStream block in MergeDataIntoBlocks())
                {
                    block.SeekToStart();

                    if (first || block.Length >= 10240)
                    {
                        byte[] blockData = block.ReadAllBytes();
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

                currentPosition = sds.Position;
                Header.XmlOffset = (uint)currentPosition;
                sds.Seek(xmlOffsetPosition, SeekOrigin.Begin);
                sds.WriteUInt32((uint)currentPosition);
                sds.Seek(headerChecksumPosition, SeekOrigin.Begin);

                byte[] hash2 = new byte[52];
                Array.Copy(BitConverter.GetBytes(Header.ResourceTypeTableOffset), 0, hash2, 0, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Header.BlockTableOffset), 0, hash2, 4, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Header.XmlOffset), 0, hash2, 8, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(SlotRamRequired), 0, hash2, 12, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(SlotVRamRequired), 0, hash2, 16, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(OtherRamRequired), 0, hash2, 20, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(OtherVRamRequired), 0, hash2, 24, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Constants.SdsHeader.Unknown32_2C), 0, hash2, 28, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes((ulong)Header.GameVersion), 0, hash2, 32, Constants.DataTypesSizes.UInt64);
                Array.Copy(BitConverter.GetBytes(Resources.Count), 0, hash2, 48, Constants.DataTypesSizes.UInt32);
                sds.WriteUInt32(FNV.Hash32(hash2));
                
                sds.Seek(currentPosition, SeekOrigin.Begin);
                sds.WriteString(XmlString);
            }
        }

        public void ExportToDirectory(string path)
        {
            throw new NotImplementedException();
        }

        public static SdsFile ImportFromDirectory(string path)
        {
            throw new NotImplementedException();
        }

        private List<MemoryStream> MergeDataIntoBlocks()
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
                mergedData.WriteUInt32(resource.Checksum);
                mergedData.Write(resource.GetRawData());
            }

            mergedData.SeekToStart();

            int numberOfBlocks = (int)mergedData.Length / Constants.SdsHeader.BlockSize;
            List<MemoryStream> dataBlocks = new List<MemoryStream>();
            for (int i = 0; i < numberOfBlocks; i++)
            {
                MemoryStream dataBlock = new MemoryStream();
                dataBlock.Write(mergedData.ReadBytes(Constants.SdsHeader.BlockSize));
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

        //public void ExtractAllFiles(string path)
        //{
        //    foreach (File file in Files)
        //        file.Extract(string.Format(@"{0}\{1}", path, file.GetSourcePath() != "not available" ? 
        //            file.GetSourcePath() : file.GetName()));
        //}

        //public void ExtractFilesByTypeName(Type type, string destPath)
        //{
        //    foreach (File file in GetFiles())
        //    {
        //        if (file.GetType() == type)
        //            file.Extract(string.Format(@"{0}\{1}", destPath, file.GetSourcePath() != "not available" ? 
        //                file.GetSourcePath() : file.GetName()));
        //    }
        //}

        //public void ExtractFileByName(string fileName, string destPah)
        //{
        //    File file = null;

        //    foreach (File _file in GetFiles())
        //    {
        //        if (_file.GetName() == fileName)
        //        {
        //            file = _file;
        //            break;
        //        }
        //    }

        //    if (file == null)
        //        throw new Exception("File not found.");

        //    file.Extract(destPah);
        //}

        //public void ReplaceFileByName(string fileName, string newFilePath)
        //{
        //    File file = null;

        //    foreach (File _file in GetFiles())
        //    {
        //        if (_file.GetName() == fileName)
        //        {
        //            file = _file;
        //            break;
        //        }
        //    }

        //    if (file == null)
        //        throw new Exception("File not found.");

        //    file.ReplaceFile(newFilePath);
        //}
    }
}
