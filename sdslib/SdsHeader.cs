using sdslib.Models;
using System;
using System.IO;

namespace sdslib
{
    public class SdsHeader
    {
        public uint Version { get; }

        public EPlatform Platform { get; set; }

        public uint ResourceTypeTableOffset { get; set; }

        public uint BlockTableOffset { get; set; }

        public uint XmlOffset { get; set; }

        public uint SlotRamRequired { get; set; }

        public uint SlotVRamRequired { get; set; }

        public uint OtherRamRequired { get; set; }

        public uint OtherVRamRequired { get; set; }

        public uint NumberOfFiles { get; set; }

        public uint Checksum
        {
            get
            {
                byte[] Bytes = new byte[52];
                Array.Copy(BitConverter.GetBytes(ResourceTypeTableOffset), 0, Bytes, 0, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(BlockTableOffset), 0, Bytes, 4, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(XmlOffset), 0, Bytes, 8, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(SlotRamRequired), 0, Bytes, 12, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(SlotVRamRequired), 0, Bytes, 16, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(OtherRamRequired), 0, Bytes, 20, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(OtherVRamRequired), 0, Bytes, 24, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Constants.SdsHeader.Unknown32_2C), 0, Bytes, 28, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Constants.SdsHeader.Unknown64_30), 0, Bytes, 32, Constants.DataTypesSizes.UInt64);
                Array.Copy(BitConverter.GetBytes(NumberOfFiles), 0, Bytes, 48, Constants.DataTypesSizes.UInt32);
                return FNV.Hash32(Bytes);
            }
        }

        public SdsHeader(string sdsFilePath)
        {
            using (FileStream fileStream = new FileStream(sdsFilePath, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length < Constants.SdsHeader.StandardHeaderSize)
                    throw new InvalidDataException("Invalid file!");

                if (fileStream.ReadString(Constants.DataTypesSizes.UInt32) != "SDS")
                    throw new Exception("This file does not contain SDS header!");

                Version = fileStream.ReadUInt32();
                if (Version > Constants.SdsHeader.Version)
                    throw new NotSupportedException("Unsupported version of SDS file!");

                Platform = (EPlatform)Enum.Parse(typeof(EPlatform), 
                    fileStream.ReadString(Constants.DataTypesSizes.UInt32));
                if (Platform != EPlatform.PC)
                    throw new NotSupportedException("Unsupported platform!");

                if (fileStream.ReadUInt32() != Constants.SdsHeader.Unknown32_C)
                    throw new Exception("Bytes do not match.");

                ResourceTypeTableOffset = fileStream.ReadUInt32();
                BlockTableOffset = fileStream.ReadUInt32();
                XmlOffset = fileStream.ReadUInt32();

                if (XmlOffset == 1049068U)
                    throw new NotSupportedException("This SDS file is encrypted.");

                SlotRamRequired = fileStream.ReadUInt32();
                SlotVRamRequired = fileStream.ReadUInt32();
                OtherRamRequired = fileStream.ReadUInt32();
                OtherVRamRequired = fileStream.ReadUInt32();

                if (fileStream.ReadUInt32() != Constants.SdsHeader.Unknown32_2C)
                    throw new Exception("Bytes do not match.");

                if (fileStream.ReadUInt64() != Constants.SdsHeader.Unknown64_30)
                    throw new Exception("Bytes do not match.");

                // Skipping of null bytes
                fileStream.Seek(Constants.DataTypesSizes.UInt64, SeekOrigin.Current);

                NumberOfFiles = fileStream.ReadUInt32();

                uint checksum = fileStream.ReadUInt32();

                if (Checksum != checksum)
                    throw new Exception("Checksum difference!");
            }
        }
    }
}
