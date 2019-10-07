namespace sdslib
{
    public class Texture : File
    {
        byte[] Unknown32;
        byte[] Unknown32_;
        byte[] Unknown16;

        public Texture(FileHeader header, string name, byte[] data)
            : base(header, name, data)
        {
            Data.SeekToStart();
            Unknown32 = Data.ReadBytes(Constants.DataTypesSizes.UInt32);
            Unknown32_ = Data.ReadBytes(Constants.DataTypesSizes.UInt32);
            Unknown16 = Data.ReadBytes(Constants.DataTypesSizes.UInt16);
            AdditionalHeaderSize = (Constants.DataTypesSizes.UInt32 * 2) + Constants.DataTypesSizes.UInt16;
            Data.SeekToStart();
        }

        public override string GetSourcePath()
        {
            return Name;
        }
    }
}
