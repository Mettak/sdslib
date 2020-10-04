namespace sdslib.ResourceTypes
{
    public interface IResource
    {
        byte[] Data { get; set; }

        byte[] Serialize();

        void ReplaceData(string path);

        void Extract(string destination);
    }
}
