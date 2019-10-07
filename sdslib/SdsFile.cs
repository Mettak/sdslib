using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using zlib;

namespace sdslib
{
    /// <summary>
    /// 
    /// </summary>
    public class SdsFile : SdsHeader
    {
        List<File> Files;
        public ReadOnlyCollection<File> GetFiles() { return Files.AsReadOnly(); }
        List<string> ResourceTypeNames;
        public ReadOnlyCollection<string> GetResourceTypeNames() { return ResourceTypeNames.AsReadOnly(); }

        public string Path { get; }

        public SdsFile(string sdsPath)
            : base(sdsPath)
        {
            Path = sdsPath;

            // Gets resource type names
            using (FileStream fileStream = new FileStream(Path, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(GetResourceTypeTableOffset(), SeekOrigin.Begin);
                uint numberOfResources = fileStream.ReadUInt32();
                ResourceTypeNames = new List<string>((int)numberOfResources);

                for (int i = 0; i < numberOfResources; i++)
                {
                    fileStream.Seek(Constants.DataTypesSizes.UInt32, SeekOrigin.Current);
                    uint resourceLenght = fileStream.ReadUInt32();
                    ResourceTypeNames.Add(fileStream.ReadString((int)resourceLenght));
                    fileStream.Seek(Constants.DataTypesSizes.UInt32, SeekOrigin.Current);
                }
            }

            //Gets decompressed data from SDS file
            MemoryStream DecompressedData;
            using (FileStream fileStream = new FileStream(Path, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(GetBlockTableOffset(), SeekOrigin.Begin);

                if (fileStream.ReadUInt32() != 1819952469U)
                    throw new Exception("Invalid SDS file!");

                fileStream.Seek(5, SeekOrigin.Current);

                DecompressedData = new MemoryStream();
                while (true)
                {
                    uint blockSize = fileStream.ReadUInt32();
                    uint blockType = fileStream.ReadUInt8();

                    if (blockSize == 0U)
                        break;

                    if (blockType == 1)
                    {
                        fileStream.Seek(16, SeekOrigin.Current);
                        uint compressedBlockSize = fileStream.ReadUInt32();

                        if (blockSize - 32U != compressedBlockSize)
                            throw new Exception("Invalid block!");

                        fileStream.Seek(12, SeekOrigin.Current);
                        byte[] compressedBlock = new byte[compressedBlockSize];
                        fileStream.Read(compressedBlock, 0, compressedBlock.Length);

                        ZOutputStream decompressStream = new ZOutputStream(DecompressedData);
                        decompressStream.Write(compressedBlock, 0, compressedBlock.Length);
                        decompressStream.finish();
                    }

                    else if (blockType == 0)
                    {
                        byte[] decompressedBlock = new byte[blockSize];
                        fileStream.Read(decompressedBlock, 0, decompressedBlock.Length);
                        DecompressedData.Write(decompressedBlock, 0, decompressedBlock.Length);
                    }

                    else
                    {
                        throw new Exception("Invalid block type!");
                    }
                }

                DecompressedData.SeekToStart();
            }

            if (DecompressedData == null)
                throw new Exception("SDS file does not contain any data!");

            // Gets SDS file names from XML file which is located in the end of the file
            List<string> fileNames;
            using (FileStream fileStream = new FileStream(Path, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(GetXmlOffset(), SeekOrigin.Begin);
                byte[] xmlBytes = fileStream.ReadBytes((int)fileStream.Length - (int)fileStream.Position);
                using (MemoryStream memoryStream = new MemoryStream(xmlBytes))
                using (XmlReader xmlReader = XmlReader.Create(memoryStream))
                {
                    fileNames = new List<string>((int)GetNumberOfFiles());
                    string typeName = string.Empty;
                    string prevTypeName = string.Empty;
                    int typeNameCounter = 0;
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            if (xmlReader.Name == "TypeName")
                            {
                                typeName = xmlReader.ReadElementContentAsString();
                            }

                            if (xmlReader.Name == "SourceDataDescription")
                            {
                                string sourceDataDecription = xmlReader.ReadElementContentAsString();

                                if (sourceDataDecription == "not available")
                                {
                                    if (prevTypeName != typeName)
                                        typeNameCounter = 0;

                                    else
                                        typeNameCounter++;

                                    fileNames.Add(string.Format("{0}_{1}.bin", typeName, typeNameCounter));
                                    prevTypeName = typeName;
                                }

                                else
                                {
                                    fileNames.Add(System.IO.Path.GetFileName(sourceDataDecription));
                                }
                            }
                        }
                    }
                }
            }

            if (fileNames.Count != GetNumberOfFiles())
                throw new Exception("Error while getting file names");

            Files = new List<File>((int)GetNumberOfFiles());
            for (int i = 0; i < GetNumberOfFiles(); i++)
            {
                FileHeader fileHeader = new FileHeader(DecompressedData.ReadUInt32(), DecompressedData.ReadUInt32(), DecompressedData.ReadUInt16(),
                    DecompressedData.ReadUInt32(), DecompressedData.ReadUInt32(), DecompressedData.ReadUInt32(), DecompressedData.ReadUInt32(), DecompressedData.ReadUInt32());

                byte[] fileData = DecompressedData.ReadBytes((int)(fileHeader.GetFileSize() - 30U));

                switch (GetResourceTypeNameByID(fileHeader.GetTypeID()))
                {
                    case "Animation2":
                        Files.Add(new Animation2(fileHeader, fileNames[i], fileData));
                        break;

                    case "Cutscene":
                        Files.Add(new Cutscene(fileHeader, fileData));
                        break;

                    case "MemFile":
                        Files.Add(new MemFile(fileHeader, fileNames[i], fileData));
                        break;

                    case "Mipmap":
                        Files.Add(new MimMap(fileHeader, fileNames[i], fileData));
                        break;

                    case "Script":
                        Files.Add(new Script(fileHeader, fileNames[i], fileData));
                        break;

                    case "Sound":
                        Files.Add(new Sound(fileHeader, fileNames[i], fileData));
                        break;

                    case "Texture":
                        Files.Add(new Texture(fileHeader, fileNames[i], fileData));
                        break;

                    case "XML":
                        Files.Add(new XML(fileHeader, fileNames[i], fileData));
                        break;

                    default:
                        Files.Add(new File(fileHeader, fileNames[i], fileData));
                        break;
                }
            }

            DecompressedData.Close();
        }

        protected string GetResourceTypeNameByID(uint resourceTypeID)
        {
            if (ResourceTypeNames == null)
                throw new Exception("");

            if (resourceTypeID > ResourceTypeNames.Count - 1)
                throw new IndexOutOfRangeException(resourceTypeID.ToString() + " is out of range!");

            return ResourceTypeNames[(int)resourceTypeID];
        }

        public void Save()
        {
            Save(Path);
        }

        public void Save(string destinationPath)
        {
            if (Files == null)
                throw new Exception("No files in the SDS file.");

            using (FileStream sds = new FileStream(destinationPath, FileMode.Create, FileAccess.ReadWrite))
            {
                SetSlotRamRequired(0U);
                SetSlotVRamRequired(0U);
                SetOtherRamRequired(0U);
                SetOtherVRamRequired(0U);
                foreach (var File in Files)
                {
                    SetSlotRamRequired(GetSlotRamRequired() + File.GetSlotRamRequired());
                    SetSlotVRamRequired(GetSlotVRamRequired() + File.GetSlotVRamRequired());
                    SetOtherRamRequired(GetOtherRamRequired() + File.GetOtherRamRequired());
                    SetOtherVRamRequired(GetOtherVRamRequired() + File.GetOtherVRamRequired());
                }

                sds.WriteString("SDS", Constants.DataTypesSizes.UInt32);
                sds.WriteUInt32(GetVersion());
                sds.WriteString(GetPlatform(), Constants.DataTypesSizes.UInt32);
                sds.WriteUInt32(Constants.SdsHeader.Unknown32_C);
                sds.WriteUInt32(GetResourceTypeTableOffset());
                sds.WriteUInt32(GetBlockTableOffset());
                sds.Seek(Constants.DataTypesSizes.UInt32, SeekOrigin.Current);
                sds.WriteUInt32(GetSlotRamRequired());
                sds.WriteUInt32(GetSlotVRamRequired());
                sds.WriteUInt32(GetOtherRamRequired());
                sds.WriteUInt32(GetOtherVRamRequired());
                sds.WriteUInt32(Constants.SdsHeader.Unknown32_2C);
                sds.WriteUInt64(Constants.SdsHeader.Unknown64_30);
                sds.WriteUInt64(Constants.SdsHeader.Uknown64_38);
                sds.WriteUInt32(GetNumberOfFiles());
                sds.Seek(Constants.DataTypesSizes.UInt32, SeekOrigin.Current);

                sds.WriteUInt32((uint)ResourceTypeNames.Count);
                for (int i = 0; i < ResourceTypeNames.Count; i++)
                {
                    sds.WriteUInt32((uint)i);
                    sds.WriteUInt32((uint)ResourceTypeNames[i].Length);
                    sds.WriteString(ResourceTypeNames[i], ResourceTypeNames[i].Length);

                    if (ResourceTypeNames[i] == "IndexBufferPool" || ResourceTypeNames[i] == "PREFAB")
                        sds.Write(new byte[] { 0x03, 0x00, 0x00, 0x00 });

                    else if (ResourceTypeNames[i] == "VertexBufferPool")
                        sds.Write(new byte[] { 0x02, 0x00, 0x00, 0x00 });

                    else
                        sds.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                }

                sds.Write(new byte[] { 0x55, 0x45, 0x7A, 0x6C, 0x00, 0x40, 0x00, 0x00, 0x04 });
                int index = 0;
                foreach (MemoryStream block in MergeDataIntoBlocks())
                {
                    block.SeekToStart();

                    if (block.Length >= 10240 || index == 0)
                    {
                        byte[] blockData = block.ReadAllBytes();
                        MemoryStream compressedBlock = new MemoryStream();
                        ZOutputStream compressStream = new ZOutputStream(compressedBlock, zlibConst.Z_BEST_COMPRESSION);
                        compressStream.Write(blockData, 0, blockData.Length);
                        compressStream.finish();
                        sds.WriteUInt32((uint)compressStream.TotalOut + 32U);
                        sds.WriteUInt8(1);
                        sds.WriteUInt32((uint)block.Length);
                        sds.Write(new byte[] { 0x20, 0x00, 0x00, 0x00, 0x00, 0x40, 0x01, 0x00, 0x01, 0x00, 0x0F, 0x08 });
                        sds.WriteUInt32((uint)compressStream.TotalOut);
                        sds.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                        compressedBlock.SeekToStart();
                        sds.Write(compressedBlock.ReadAllBytes());
                    }

                    else
                    {
                        sds.WriteUInt32((uint)block.Length);
                        sds.WriteUInt8(0);
                        sds.Write(block.ReadAllBytes());
                    }

                    index++;
                }

                sds.Write(new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0 });

                SetXmlOffset((uint)sds.Position);
                sds.Seek(24, SeekOrigin.Begin);
                sds.WriteUInt32(GetXmlOffset());

                SetChecksum(CalculateChecksum());
                sds.Seek(68U, SeekOrigin.Begin);
                sds.WriteUInt32(GetChecksum());

                sds.Seek(GetXmlOffset(), SeekOrigin.Begin);

                sds.WriteString(CreateXML());
            }

            string CreateXML()
            {
                string xmlFile = "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\" ?>\n<xml>\n";

                foreach (var File in Files)
                {
                    xmlFile += "\t<ResourceInfo>\n\t\t<CustomDebugInfo/>\n\t\t";
                    xmlFile += "<TypeName>" + GetResourceTypeNameByID(File.GetTypeID()) + "</TypeName>\n\t\t";
                    xmlFile += "<SourceDataDescription>" + File.GetSourcePath() + "</SourceDataDescription>\n\t\t";
                    xmlFile += "<SlotRamRequired __type='Int'>" + File.GetSlotRamRequired() + "</SlotRamRequired>\n\t\t";
                    xmlFile += "<SlotVramRequired __type='Int'>" + File.GetSlotVRamRequired() + "</SlotVramRequired>\n\t\t";
                    xmlFile += "<OtherRamRequired __type='Int'>" + File.GetOtherRamRequired() + "</OtherRamRequired>\n\t\t";
                    xmlFile += "<OtherVramRequired __type='Int'>" + File.GetOtherVRamRequired() + "</OtherVramRequired>\n\t";
                    xmlFile += "</ResourceInfo>\n";
                }

                xmlFile += "</xml>\n";

                return xmlFile.Replace("\n", Environment.NewLine);
            }

            List<MemoryStream> MergeDataIntoBlocks()
            {
                if (Files == null)
                    throw new Exception("");

                MemoryStream mergedData = new MemoryStream();
                foreach (var file in Files)
                {
                    mergedData.WriteUInt32(file.GetTypeID());
                    mergedData.WriteUInt32(file.GetFileSize());
                    mergedData.WriteUInt16(file.GetVersion());
                    mergedData.WriteUInt32(file.GetSlotRamRequired());
                    mergedData.WriteUInt32(file.GetSlotVRamRequired());
                    mergedData.WriteUInt32(file.GetOtherRamRequired());
                    mergedData.WriteUInt32(file.GetOtherVRamRequired());
                    mergedData.WriteUInt32(file.GetChecksum());
                    mergedData.Write(file.GetData());
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
        }
    }
}
