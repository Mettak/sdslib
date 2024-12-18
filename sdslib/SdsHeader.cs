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
        public const int HeaderSize = 72;

        public const uint MinSupportedVersion = 19U;

        public const uint MaxSupportedVersion = 20U;

        public const uint Encrypted = 1049068U;

        public const uint Unknown32_2C = 1U;

        public string Name { get; set; }

        public uint Version { get; set; }

        public EPlatform Platform { get; set; } = EPlatform.PC;

        [JsonIgnore]
        public uint ResourceTypeTableOffset { get; set; } = 72;

        [JsonIgnore]
        public uint BlockTableOffset { get; set; }

        [JsonIgnore]
        public uint XmlOffset { get; set; }

        public EGameVersion GameVersion { get; set; }

        public static SdsHeader FromFile(string sdsFilePath)
        {
            SdsHeader header = new SdsHeader();
            header.Name = Path.GetFileName(sdsFilePath);

            using (FileStream fileStream = new FileStream(sdsFilePath, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length < HeaderSize)
                {
                    throw new InvalidDataException("Invalid file!");
                }

                if (fileStream.ReadString(sizeof(uint)) != "SDS")
                {
                    throw new InvalidDataException("This file does not contain SDS header!");
                }

                header.Version = fileStream.ReadUInt32();
                if (header.Version < MinSupportedVersion || header.Version > MaxSupportedVersion)
                {
                    throw new NotSupportedException($"Version {header.Version} not supported");
                }

                string platformString = fileStream.ReadString(sizeof(uint));
                if (Enum.TryParse(platformString, out EPlatform platform))
                {
                    header.Platform = platform;
                }

                else
                {
                    throw new InvalidDataException(platformString);
                }

                if (header.Platform != EPlatform.PC) // In future will be added multiplatform support
                {
                    throw new NotSupportedException($"Platform {platform} not supported");
                }

                uint hash = fileStream.ReadUInt32();
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.WriteString("SDS", sizeof(uint));
                    ms.WriteUInt32(header.Version);
                    ms.WriteString(header.Platform.ToString(), sizeof(uint));
                    var computedHash = FNV.Hash32(ms.ReadAllBytes());
                    if (computedHash != hash)
                    {
                        throw new InvalidDataException("Checksum difference.");
                    }
                }

                header.ResourceTypeTableOffset = fileStream.ReadUInt32();
                header.BlockTableOffset = fileStream.ReadUInt32();
                header.XmlOffset = fileStream.ReadUInt32();

                if (header.Version == 19U &&
                    header.XmlOffset == Encrypted)
                {
                    throw new NotSupportedException("This SDS file is encrypted.");
                }

                uint slotRamRequired = fileStream.ReadUInt32();
                uint slotVRamRequired = fileStream.ReadUInt32();
                uint otherRamRequired = fileStream.ReadUInt32();
                uint otherVRamRequired = fileStream.ReadUInt32();

                if (fileStream.ReadUInt32() != SdsHeader.Unknown32_2C)
                {
                    throw new Exception("Bytes do not match.");
                }

                header.GameVersion = (EGameVersion)fileStream.ReadUInt64();

                if (header.GameVersion != EGameVersion.Classic && header.GameVersion != EGameVersion.DefinitiveEdition)
                {
                    throw new NotSupportedException(header.GameVersion.ToString());
                }

                // Skipping of null bytes
                fileStream.Seek(sizeof(ulong), SeekOrigin.Current);

                uint numberOfFiles = fileStream.ReadUInt32();

                uint checksum = fileStream.ReadUInt32();

                uint calculatedChecksum = FNV.Hash32(header.ResourceTypeTableOffset, header.BlockTableOffset, header.XmlOffset, slotRamRequired,
                    slotVRamRequired, otherRamRequired, otherVRamRequired, Unknown32_2C, (ulong)header.GameVersion, 0UL, numberOfFiles);

                if (calculatedChecksum != checksum)
                    throw new Exception("Checksum difference!");
            }

            return header;
        }
    }
}
