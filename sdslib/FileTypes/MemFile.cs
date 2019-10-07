namespace sdslib
{
    public class MemFile : File
    {
        private uint SourcePathSize;
        private string SourcePath;
        private byte[] Unknown32 = { 0x1, 0x0, 0x0, 0x0 };
        private uint MemFileSize;

        public MemFile(FileHeader header, string name, byte[] data)
            : base(header, name, data)
        {
            Data.SeekToStart();
            SourcePathSize = Data.ReadUInt32();
            SourcePath = Data.ReadString((int)SourcePathSize);
            Unknown32 = Data.ReadBytes(Constants.DataTypesSizes.UInt32);
            MemFileSize = Data.ReadUInt32();
            AdditionalHeaderSize = (Constants.DataTypesSizes.UInt32 * 3) + (uint)SourcePath.Length;
            Data.SeekToStart();
        }

        public override string GetSourcePath()
        {
            return SourcePath;
        }
    }
}
