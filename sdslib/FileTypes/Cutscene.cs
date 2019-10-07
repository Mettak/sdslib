using System.IO;

namespace sdslib
{
    public class Cutscene : File
    {
        public byte[] Unknown32 = { 0x1, 0x0, 0x0, 0x0 };

        private uint CutsceneNameLenght;
        private string CutsceneName;
        private byte[] Empty32 = { 0x0, 0x0, 0x0, 0x0 };
        private byte[] Separator = { 0x0 };

        public string GcsFileName;
        private uint GcsFileSize;
        public string SpdFileName;
        private uint SpdFileSize;

        public Cutscene(FileHeader header, byte[] data)
            : base(header, data)
        {
            Data.SeekToStart();

            Unknown32 = Data.ReadBytes(Constants.DataTypesSizes.UInt32);
            CutsceneNameLenght = Data.ReadUInt16();
            CutsceneName = Data.ReadString((int)CutsceneNameLenght);
            Name = CutsceneName + ".cutscene";
            Data.Seek(Empty32.Length + Separator.Length, System.IO.SeekOrigin.Current);
            GcsFileName = CutsceneName + ".gcs";
            GcsFileSize = Data.ReadUInt32() - 4U;
            Data.Seek(GcsFileSize + Separator.Length, System.IO.SeekOrigin.Current);
            SpdFileName = CutsceneName + ".spd";
            SpdFileSize = Data.ReadUInt32() - 4U;

            AdditionalHeaderSize = Constants.DataTypesSizes.UInt32 + Constants.DataTypesSizes.UInt16 +
                (uint)CutsceneName.Length + Constants.DataTypesSizes.UInt32;

            Data.SeekToStart();
        }

        public override void Extract(string destinationDirectory)
        {
            Data.Seek(AdditionalHeaderSize + Separator.Length + Constants.DataTypesSizes.UInt32, System.IO.SeekOrigin.Begin);

            using (FileStream gcsFile = new FileStream(destinationDirectory + @"\" + GcsFileName, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] gcsFileData = Data.ReadBytes((int)GcsFileSize);
                gcsFile.Write(gcsFileData, 0, gcsFileData.Length);
            }

            Data.Seek(Separator.Length + Constants.DataTypesSizes.UInt32, SeekOrigin.Current);

            using (FileStream spdFile = new FileStream(destinationDirectory + @"\" + SpdFileName, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] spdFileData = Data.ReadBytes((int)SpdFileSize);
                spdFile.Write(spdFileData, 0, spdFileData.Length);
            }

            Data.SeekToStart();
        }

        public uint GetGcsFileSize()
        {
            return GcsFileSize;
        }

        public uint GetSpdFileSize()
        {
            return SpdFileSize;
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}\n{2} - {3}", GcsFileName, Convert.BytesToString(GetGcsFileSize()),
                SpdFileName, Convert.BytesToString(GetSpdFileSize()));
        }
    }
}
