namespace sdslib
{
    public class MimMap : File
    {
        byte[] Unknown32;
        byte[] Unknown32_4;
        byte[] Unknown8;

        public MimMap(FileHeader header, string name, byte[] data)
            : base(header, name, data)
        {
            Name = "MIP_" + Name;
            Data.SeekToStart();
            Unknown32 = Data.ReadBytes(Constants.DataTypesSizes.UInt32);
            Unknown32_4 = Data.ReadBytes(Constants.DataTypesSizes.UInt32);
            Unknown8 = Data.ReadBytes(Constants.DataTypesSizes.UInt8);
            AdditionalHeaderSize = (Constants.DataTypesSizes.UInt32 * 2) + Constants.DataTypesSizes.UInt8;
            Data.SeekToStart();
        }

        public override string GetSourcePath()
        {
            return Name.Replace("MIP_", string.Empty);
        }
    }
}
