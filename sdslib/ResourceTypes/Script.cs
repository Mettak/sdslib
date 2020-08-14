using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class Script : Resource
    {
        public List<ScriptFile> Scripts { get; set; } = new List<ScriptFile>();

        public string Path { get; set; }

        public override uint Size => sizeof(ushort) + (uint)Path.Length + sizeof(uint) + base.Size;

        public override byte[] Data
        {
            get
            {
                List<byte> bytes = new List<byte>();
                foreach (var script in Scripts)
                {
                    script.Data.ToList().ForEach(x => bytes.Add(x));
                }
                return bytes.ToArray();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        [JsonConstructor]
        public Script() { }

        public Script(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, byte[] rawData)
        {
            Guid = System.Guid.NewGuid().ToString();
            Info = resourceInfo;
            Version = version;
            SlotRamRequired = slotRamRequired;
            SlotVRamRequired = slotVRamRequired;
            OtherRamRequired = otherRamRequired;
            OtherVRamRequired = otherVRamRequired;

            ushort pathLength = BitConverter.ToUInt16(rawData, 0);
            Path = Encoding.UTF8.GetString(rawData, sizeof(ushort), pathLength);
            uint scriptCount = BitConverter.ToUInt32(rawData, sizeof(ushort) + pathLength);

            using (MemoryStream memory = new MemoryStream(rawData.Skip(sizeof(ushort) + pathLength + sizeof(uint)).ToArray()))
            {
                for (int i = 0; i < scriptCount; i++)
                {
                    ScriptFile scriptFile = new ScriptFile();
                    ulong pathHash = memory.ReadUInt64();
                    ulong dataHash = memory.ReadUInt64();
                    ushort scriptPathLength = memory.ReadUInt16();
                    scriptFile.Path = memory.ReadString(scriptPathLength);
                    uint scriptLength = memory.ReadUInt32();
                    scriptFile.Data = memory.ReadBytes((int)scriptLength);

                    if (scriptFile.PathHash != pathHash || scriptFile.DataHash != dataHash)
                    {
                        throw new InvalidDataException();
                    }

                    Scripts.Add(scriptFile);
                }
            }
        }

        public override void Extract(string destination)
        {
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(destination)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
            }

            Scripts.ForEach(x => x.Extract($@"{destination}\{x.Path}"));
        }

        public override byte[] GetRawData()
        {
            List<byte> buffer = new List<byte>();
            buffer.Concat(BitConverter.GetBytes((ushort)Path.Length));
            buffer.Concat(Encoding.UTF8.GetBytes(Path));
            buffer.Concat(BitConverter.GetBytes((uint)Scripts.Count));
            buffer.Concat(Data);
            return buffer.ToArray();
        }
    }

    public class ScriptFile
    {
        public string Path { get; set; }

        [JsonIgnore]
        public byte[] Data { get; set; }

        [JsonIgnore]
        public ulong PathHash
        {
            get
            {
                return FNV.Hash64(Encoding.UTF8.GetBytes(Path));
            }
        }

        [JsonIgnore]
        public ulong DataHash
        {
            get
            {
                return FNV.Hash64(Data);
            }
        }

        public void Extract(string destination)
        {
            if (!Directory.Exists(System.IO. Path.GetDirectoryName(destination)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
            }

            System.IO.File.WriteAllBytes(destination, Data);
        }

        public byte[] GetRawData()
        {
            byte[] bytes = new byte[sizeof(ulong) + sizeof(ulong) + 
                sizeof(ushort) + Path.Length + sizeof(uint) + Data.Length];
            Array.Copy(BitConverter.GetBytes(PathHash), 0, bytes, 0, sizeof(ulong));
            Array.Copy(BitConverter.GetBytes(DataHash), 0, bytes, 8, sizeof(ulong));
            Array.Copy(BitConverter.GetBytes((ushort)Path.Length), 0, bytes, 16, sizeof(ushort));
            Array.Copy(Encoding.UTF8.GetBytes(Path), 0, bytes, 18, Path.Length);
            Array.Copy(Data, 0, bytes, (18 + Path.Length), Data.Length);
            return bytes;
        }
    }
}
