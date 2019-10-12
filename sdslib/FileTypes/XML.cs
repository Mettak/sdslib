namespace sdslib
{
    public class XML : File
    {
        uint TargetSystemNameLength;
        string TargetSystemName;
        byte[] Separator = { 0x0 };
        uint SourcePathLength;
        string SourcePath;

        public XML(FileHeader header, string name, byte[] data)
            : base(header, data)
        {
            Name = name + "_XML.bin";
            Data.SeekToStart();
            TargetSystemNameLength = Data.ReadUInt32();
            TargetSystemName = Data.ReadString((int)TargetSystemNameLength);
            Data.Seek(Separator.Length, System.IO.SeekOrigin.Current);
            SourcePathLength = Data.ReadUInt32();
            SourcePath = Data.ReadString((int)SourcePathLength);
            Data.SeekToStart();
        }

        public string GetTargetSystemName()
        {
            return TargetSystemName;
        }

        public override string GetSourcePath()
        {
            return SourcePath + "_XML.bin";
        }
    }
}
