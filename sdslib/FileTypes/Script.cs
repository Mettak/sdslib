using System.Collections.Generic;
using System.IO;

namespace sdslib
{
    public class Script : File
    {
        public class ScriptFile
        {
            byte[] Unknown32;
            byte[] Unknown32_;
            byte[] Unknown32__;
            byte[] Unknown32___;
            uint ScriptPathSize;
            string ScriptPath;
            uint ScriptSize;
            public uint AdditionalScriptFileHeaderSize;

            public ScriptFile(byte[] Unknown32, byte[] Unknown32_, byte[] Unknown32__, byte[] Unknown32___,
                uint ScriptPathSize, string ScriptPath, uint ScriptSize)
            {
                this.Unknown32 = Unknown32;
                this.Unknown32_ = Unknown32_;
                this.Unknown32__ = Unknown32__;
                this.Unknown32___ = Unknown32___;
                this.ScriptPathSize = ScriptPathSize;
                this.ScriptPath = ScriptPath;
                this.ScriptSize = ScriptSize;
                AdditionalScriptFileHeaderSize = (Constants.DataTypesSizes.UInt32 * 5) + 
                    Constants.DataTypesSizes.UInt16 + (uint)ScriptPath.Length;
            }

            public string GetScriptName()
            {
                return Path.GetFileName(ScriptPath);
            }

            public string GetScriptPath()
            {
                return ScriptPath;
            }

            public uint GetScriptSize()
            {
                return ScriptSize;
            }
        };

        ushort ScriptPakPathSize;
        string ScriptPakPath;
        uint NumberOfScripts;
        public List<ScriptFile> Scripts;

        public Script(FileHeader header, string name, byte[] data)
            : base(header, data)
        {
            Data.SeekToStart();
            ScriptPakPathSize = Data.ReadUInt16();

            if (ScriptPakPathSize == 0U)
            {
                ScriptPakPath = string.Empty;
                Name = Path.ChangeExtension(name, "luapak");
            }

            else
            {
                ScriptPakPath = Data.ReadString(ScriptPakPathSize);
                Name = Path.GetFileName(ScriptPakPath.Remove(ScriptPakPath.Length - 1)) + ".luapak";
            }

            NumberOfScripts = Data.ReadUInt32();
            AdditionalHeaderSize = Constants.DataTypesSizes.UInt16 + (uint)ScriptPakPath.Length + Constants.DataTypesSizes.UInt32;

            Scripts = new List<ScriptFile>((int)NumberOfScripts);
            for (int i = 0; i < NumberOfScripts; i++)
            {
                byte[] Unknown32 = Data.ReadBytes(Constants.DataTypesSizes.UInt32);
                byte[] Unknown32_ = Data.ReadBytes(Constants.DataTypesSizes.UInt32);
                byte[] Unknown32__ = Data.ReadBytes(Constants.DataTypesSizes.UInt32);
                byte[] Unknown32___ = Data.ReadBytes(Constants.DataTypesSizes.UInt32);
                uint ScriptPathSize = Data.ReadUInt16();
                string ScriptPath = Data.ReadString((int)ScriptPathSize);
                uint ScriptSize = Data.ReadUInt32();
                Scripts.Add(new ScriptFile(Unknown32, Unknown32_, Unknown32__, Unknown32___,
                    ScriptPathSize, ScriptPath, ScriptSize));
                Data.Seek(ScriptSize, SeekOrigin.Current);
            }

            Data.SeekToStart();
        }

        public override void ReplaceFile(string newFilePath)
        {
            throw new System.NotSupportedException("Not supported.");
        }

        public override void Extract(string destination)
        {
            Data.Seek(AdditionalHeaderSize, SeekOrigin.Begin);

            for (int i = 0; i < NumberOfScripts; i++)
            {
                if (!Directory.Exists(destination + @"\" + 
                    Path.GetDirectoryName(Scripts[i].GetScriptPath())))
                    Directory.CreateDirectory(destination + @"\" + 
                        Path.GetDirectoryName(Scripts[i].GetScriptPath()));

            using (FileStream script = new FileStream(destination + @"\" + Scripts[i].GetScriptPath(),
                    FileMode.CreateNew, FileAccess.Write))
                {
                    Data.Seek(Scripts[i].AdditionalScriptFileHeaderSize, SeekOrigin.Current);
                    byte[] scriptData = Data.ReadBytes((int)Scripts[i].GetScriptSize());
                    script.Write(scriptData, 0, scriptData.Length);
                }
            }

            Data.SeekToStart();
        }

        public override string ToString()
        {
            string scriptList = string.Empty;
            for (int i = 0; i < NumberOfScripts; i++)
            {
                scriptList += string.Format("{0} - {1}", Scripts[i].GetScriptName(),
                    Convert.BytesToString(Scripts[i].GetScriptSize()));

                if (i != NumberOfScripts - 1)
                    scriptList += "\n";
            }

            return scriptList;
        }
    }
}
