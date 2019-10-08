using System.IO;

namespace sdslib
{
    public class File : FileHeader
    {
        protected uint AdditionalHeaderSize = 0U;
        public uint GetAdditionalHeaderSize() { return AdditionalHeaderSize; }

        protected MemoryStream Data;
        public byte[] GetData() { return Data.ReadAllBytes(); }

        protected string Name;
        public string GetName() { return Name; }

        public File(FileHeader Header, byte[] Data)
            : base(Header)
        {
            this.Data = new MemoryStream(Data);
            Name = string.Empty;
        }

        public File(FileHeader Header, string Name, byte[] Data)
            : base(Header)
        {
            this.Data = new MemoryStream(Data);
            this.Name = Name;
        }

        public virtual void Extract(string destination)
        {
            if (!Directory.Exists(Path.GetDirectoryName(destination)))
                Directory.CreateDirectory(Path.GetDirectoryName(destination));

            using (FileStream file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write))
            {
                Data.Seek(AdditionalHeaderSize, SeekOrigin.Begin);
                byte[] data = Data.ReadBytes((int)GetSize());
                file.Write(data, 0, data.Length);
                Data.SeekToStart();
            }
        }

        public uint GetSize()
        {
            return (uint)Data.Length - AdditionalHeaderSize;
        }

        public virtual string GetSourcePath()
        {
            return "not available";
        }

        public virtual void ReplaceFile(string newFilePath)
        {
            using (FileStream newFile = new FileStream(newFilePath, FileMode.Open, FileAccess.Read))
            {
                Data.SeekToStart();
                byte[] AdditionalHeaderBytes = Data.ReadBytes((int)AdditionalHeaderSize);
                Data.Close();
                Data = new MemoryStream();
                Data.SeekToStart();
                Data.Write(AdditionalHeaderBytes);
                Data.Write(newFile.ReadAllBytes());
                Data.SeekToStart();
                SetFileSize(AdditionalHeaderSize + (uint)newFile.Length + 30U);
                SetSlotVRamRequired((uint)newFile.Length);
                UpdateChecksum();
            }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Convert.BytesToString(GetSize()));
        }
    }
}
