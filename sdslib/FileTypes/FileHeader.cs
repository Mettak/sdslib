using System;

namespace sdslib
{
    public class FileHeader
    {
        private uint TypeID;
        public uint GetTypeID() { return TypeID; }

        private uint FileSize;
        public uint GetFileSize() { return FileSize; }
        protected void SetFileSize(uint fileSize) { FileSize = fileSize; }

        private ushort Version;
        public ushort GetVersion() { return Version; }

        private uint SlotRamRequired;
        public uint GetSlotRamRequired() { return SlotRamRequired; }
        protected void SetSlotRamRequired(uint slotRamRequired) { SlotRamRequired = slotRamRequired; }

        private uint SlotVRamRequired;
        public uint GetSlotVRamRequired() { return SlotVRamRequired; }
        protected void SetSlotVRamRequired(uint slotVRamRequired) { SlotVRamRequired = slotVRamRequired; }

        private uint OtherRamRequired;
        public uint GetOtherRamRequired() { return OtherRamRequired; }
        protected void SetOtherRamRequired(uint otherRamRequired) { OtherRamRequired = otherRamRequired; }

        private uint OtherVRamRequired;
        public uint GetOtherVRamRequired() { return OtherVRamRequired; }
        protected void SetOtherVRamRequired(uint otherVRamRequired) { OtherVRamRequired = otherVRamRequired; }

        private uint Checksum;
        public uint GetChecksum() { return Checksum; }

        public FileHeader(uint TypeID, uint FileSize, ushort Version,
            uint SlotRamRequired, uint SlotVRamRequired, uint OtherRamRequired, uint OtherVRamRequired, uint Checksum)
        {
            this.TypeID = TypeID;
            this.FileSize = FileSize;
            this.Version = Version;
            this.SlotRamRequired = SlotRamRequired;
            this.SlotVRamRequired = SlotVRamRequired;
            this.OtherRamRequired = OtherRamRequired;
            this.OtherVRamRequired = OtherVRamRequired;
            this.Checksum = Checksum;
        }

        public FileHeader(FileHeader header)
        {
            TypeID = header.TypeID;
            FileSize = header.FileSize;
            Version = header.Version;
            SlotRamRequired = header.SlotRamRequired;
            SlotVRamRequired = header.SlotVRamRequired;
            OtherRamRequired = header.OtherRamRequired;
            OtherVRamRequired = header.OtherVRamRequired;
            Checksum = header.Checksum;
        }

        protected void UpdateChecksum()
        {
            byte[] Bytes = new byte[26];
            Array.Copy(BitConverter.GetBytes(TypeID), 0, Bytes, 0, Constants.DataTypesSizes.UInt32);
            Array.Copy(BitConverter.GetBytes(FileSize), 0, Bytes, 4, Constants.DataTypesSizes.UInt32);
            Array.Copy(BitConverter.GetBytes(Version), 0, Bytes, 8, Constants.DataTypesSizes.UInt16);
            Array.Copy(BitConverter.GetBytes(SlotRamRequired), 0, Bytes, 10, Constants.DataTypesSizes.UInt32);
            Array.Copy(BitConverter.GetBytes(SlotVRamRequired), 0, Bytes, 14, Constants.DataTypesSizes.UInt32);
            Array.Copy(BitConverter.GetBytes(OtherRamRequired), 0, Bytes, 18, Constants.DataTypesSizes.UInt32);
            Array.Copy(BitConverter.GetBytes(OtherVRamRequired), 0, Bytes, 22, Constants.DataTypesSizes.UInt32);
            Checksum = FNV.Hash32(Bytes);
        }
    }
}
