using Newtonsoft.Json;
using sdslib.Enums;
using sdslib.ResourceTypes;
using System;
using System.Collections.Generic;
using System.IO;

namespace sdslib
{
    public class SdsHeader
    {
        public string Name { get; set; }

        [JsonIgnore]
        public uint Version { get; set; } = 19;

        public EPlatform Platform { get; set; } = EPlatform.PC;

        [JsonIgnore]
        public uint ResourceTypeTableOffset { get; set; } = 72;

        [JsonIgnore]
        public uint BlockTableOffset { get; set; }

        [JsonIgnore]
        public uint XmlOffset { get; set; }

        public EGameVersion GameVersion { get; set; } = EGameVersion.DefinitiveEdition;

        public List<ResourceType> ResourceTypes { get; set; } = new List<ResourceType>();

        public static SdsHeader FromFile(string sdsFilePath)
        {
            SdsHeader header = new SdsHeader();

            header.Name = Path.GetFileName(sdsFilePath);

            using (FileStream fileStream = new FileStream(sdsFilePath, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length < Constants.SdsHeader.StandardHeaderSize)
                    throw new InvalidDataException("Invalid file!");

                if (fileStream.ReadString(sizeof(uint)) != "SDS")
                    throw new Exception("This file does not contain SDS header!");

                header.Version = fileStream.ReadUInt32();
                if (header.Version > Constants.SdsHeader.Version)
                    throw new NotSupportedException("Unsupported version of SDS file!");

                if (Enum.TryParse(fileStream.ReadString(sizeof(uint)), out EPlatform platform))
                {
                    header.Platform = platform;
                }

                else
                {
                    throw new InvalidDataException();
                }

                if (header.Platform != EPlatform.PC)
                    throw new NotSupportedException("Unsupported platform!");

                if (fileStream.ReadUInt32() != Constants.SdsHeader.Unknown32_C)
                    throw new Exception("Bytes do not match.");

                header.ResourceTypeTableOffset = fileStream.ReadUInt32();
                header.BlockTableOffset = fileStream.ReadUInt32();
                header.XmlOffset = fileStream.ReadUInt32();

                if (header.XmlOffset == Constants.SdsHeader.Encrypted)
                    throw new NotSupportedException("This SDS file is encrypted.");

                uint slotRamRequired = fileStream.ReadUInt32();
                uint slotVRamRequired = fileStream.ReadUInt32();
                uint otherRamRequired = fileStream.ReadUInt32();
                uint otherVRamRequired = fileStream.ReadUInt32();

                if (fileStream.ReadUInt32() != Constants.SdsHeader.Unknown32_2C)
                    throw new Exception("Bytes do not match.");

                header.GameVersion = (EGameVersion)fileStream.ReadUInt64();

                if (header.GameVersion != EGameVersion.Classic && header.GameVersion != EGameVersion.DefinitiveEdition)
                    throw new NotSupportedException(header.GameVersion.ToString());

                // Skipping of null bytes
                fileStream.Seek(sizeof(ulong), SeekOrigin.Current);

                uint numberOfFiles = fileStream.ReadUInt32();

                uint checksum = fileStream.ReadUInt32();

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.WriteUInt32(header.ResourceTypeTableOffset);
                    ms.WriteUInt32(header.BlockTableOffset);
                    ms.WriteUInt32(header.XmlOffset);
                    ms.WriteUInt32(slotRamRequired);
                    ms.WriteUInt32(slotVRamRequired);
                    ms.WriteUInt32(otherRamRequired);
                    ms.WriteUInt32(otherVRamRequired);
                    ms.WriteUInt32(Constants.SdsHeader.Unknown32_2C);
                    ms.WriteUInt64((ulong)header.GameVersion);
                    ms.WriteUInt64(0);
                    ms.WriteUInt32(numberOfFiles);

                    uint calculatedChecksum = FNV.Hash32(ms.ReadAllBytes());

                    if (calculatedChecksum != checksum)
                        throw new Exception("Checksum difference!");
                }

                fileStream.Seek(header.ResourceTypeTableOffset, SeekOrigin.Begin);
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
                    header.ResourceTypes.Add(resourceType);
                }
            }

            return header;
        }
    }
}
