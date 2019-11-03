namespace sdslib
{
    public class Animation2 : File
    {
        public Animation2(FileHeader header, string name, byte[] data)
            : base(header, name, data)
        {
            Name += ".anim2";
        }

        public override string GetSourcePath()
        {
            return Name;
        }
    }
}
