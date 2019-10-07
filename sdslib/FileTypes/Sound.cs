namespace sdslib
{
    public class Sound : File
    {
        byte SourcePathSize;
        string SourcePath;
        uint FsbFileSize;

        public Sound(FileHeader header, string name, byte[] data)
            : base(header, name, data)
        {
            Name += ".fsb";
            Data.SeekToStart();
            SourcePathSize = Data.ReadUInt8();
            SourcePath = Data.ReadString(SourcePathSize);
            FsbFileSize = Data.ReadUInt32();
            AdditionalHeaderSize = Constants.DataTypesSizes.UInt8 + (uint)SourcePath.Length + Constants.DataTypesSizes.UInt32;
            Data.SeekToStart();
        }

        public override string GetSourcePath()
        {
            return SourcePath;
        }
    }
}
