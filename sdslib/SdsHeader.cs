using System;
using System.IO;

namespace sdslib
{
    public class SdsHeader
    {
        private string SdsFileName;
        public string GetSdsFileName() { return SdsFileName; }
        protected void SetSdsFileName(string sdsFileName) { SdsFileName = sdsFileName; }

        private uint Version;
        public uint GetVersion() { return Version; }

        private string Platform;
        public string GetPlatform() { return Platform; }

        private uint ResourceTypeTableOffset;
        protected uint GetResourceTypeTableOffset() { return ResourceTypeTableOffset; }

        private uint BlockTableOffset;
        protected uint GetBlockTableOffset() { return BlockTableOffset; }
        protected void SetBlockTableOffset(uint blockTableOffset) { BlockTableOffset = blockTableOffset; }

        private uint XmlOffset;
        protected uint GetXmlOffset() { return XmlOffset; }
        protected void SetXmlOffset(uint xmlOffset) { XmlOffset = xmlOffset; }

        private uint SlotRamRequired;
        protected uint GetSlotRamRequired() { return SlotRamRequired; }
        protected void SetSlotRamRequired(uint slotRamRequired) { SlotRamRequired = slotRamRequired; }

        private uint SlotVRamRequired;
        protected uint GetSlotVRamRequired() { return SlotVRamRequired; }
        protected void SetSlotVRamRequired(uint slotVRamRequired) { SlotVRamRequired = slotVRamRequired; }

        private uint OtherRamRequired;
        protected uint GetOtherRamRequired() { return OtherRamRequired; }
        protected void SetOtherRamRequired(uint otherRamRequired) { OtherRamRequired = otherRamRequired; }

        private uint OtherVRamRequired;
        protected uint GetOtherVRamRequired() { return OtherVRamRequired; }
        protected void SetOtherVRamRequired(uint otherVRamRequired) { OtherVRamRequired = otherVRamRequired; }

        private uint NumberOfFiles;
        public uint GetNumberOfFiles() { return NumberOfFiles; }

        private uint Checksum;
        protected uint GetChecksum() { return Checksum; }
        protected void SetChecksum(uint checksum) { Checksum = checksum; }
        protected uint CalculateChecksum()
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

        public SdsHeader(string sdsFilePath)
        {
            using (FileStream fileStream = new FileStream(sdsFilePath, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length < Constants.SdsHeader.StandardHeaderSize)
                    throw new InvalidDataException("Invalid file!");

                if (fileStream.ReadString(Constants.DataTypesSizes.UInt32) != "SDS")
                    throw new Exception("This file does not contain SDS header!");

                SdsFileName = Path.GetFileNameWithoutExtension(sdsFilePath);

                Version = fileStream.ReadUInt32();
                if (Version > Constants.SdsHeader.Version)
                    throw new NotSupportedException("Unsupported version of SDS file!");

                Platform = fileStream.ReadString(Constants.DataTypesSizes.UInt32);
                if (Platform != "PC")
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

                Checksum = fileStream.ReadUInt32();

                if (Checksum != CalculateChecksum())
                    throw new Exception("Checksum difference!");
            }
        }
    }
}
