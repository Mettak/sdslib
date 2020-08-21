using Newtonsoft.Json;
using sdslib.Enums;
using sdslib.ResourceTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

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

                if (fileStream.ReadString(Constants.DataTypesSizes.UInt32) != "SDS")
                    throw new Exception("This file does not contain SDS header!");

                header.Version = fileStream.ReadUInt32();
                if (header.Version > Constants.SdsHeader.Version)
                    throw new NotSupportedException("Unsupported version of SDS file!");

                if (Enum.TryParse(fileStream.ReadString(Constants.DataTypesSizes.UInt32), out EPlatform platform))
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
                    throw new NotSupportedException();

                // Skipping of null bytes
                fileStream.Seek(Constants.DataTypesSizes.UInt64, SeekOrigin.Current);

                uint numberOfFiles = fileStream.ReadUInt32();

                uint checksum = fileStream.ReadUInt32();

                byte[] bytes = new byte[52];
                Array.Copy(BitConverter.GetBytes(header.ResourceTypeTableOffset), 0, bytes, 0, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(header.BlockTableOffset), 0, bytes, 4, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(header.XmlOffset), 0, bytes, 8, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(slotRamRequired), 0, bytes, 12, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(slotVRamRequired), 0, bytes, 16, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(otherRamRequired), 0, bytes, 20, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(otherVRamRequired), 0, bytes, 24, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Constants.SdsHeader.Unknown32_2C), 0, bytes, 28, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes((ulong)header.GameVersion), 0, bytes, 32, Constants.DataTypesSizes.UInt64);
                Array.Copy(BitConverter.GetBytes(numberOfFiles), 0, bytes, 48, Constants.DataTypesSizes.UInt32);
                uint calculatedChecksum = FNV.Hash32(bytes);

                if (calculatedChecksum != checksum)
                    throw new Exception("Checksum difference!");

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
                        throw new InvalidDataException();
                    }
                    header.ResourceTypes.Add(resourceType);
                }
            }

            return header;
        }
    }
}
