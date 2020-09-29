using AutoMapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace sdslib.ResourceTypes
{
    public class Script : Resource
    {
        public List<ScriptFile> Scripts { get; set; } = new List<ScriptFile>();

        public string Path { get; set; }

        public override byte[] Data
        {
            get
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    foreach (var script in Scripts)
                    {
                        byte[] scriptData = script.Serialize();
                        memory.Write(scriptData, 0, scriptData.Length);
                    }

                    return memory.ReadAllBytes();
                }
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public new static Script Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, uint? unknown32, uint? unknown32_2, byte[] rawData, IMapper mapper)
        {
            Script script = new Script
            {
                Guid = System.Guid.NewGuid().ToString(),
                Info = resourceInfo,
                Version = version,
                SlotRamRequired = slotRamRequired,
                SlotVRamRequired = slotVRamRequired,
                OtherRamRequired = otherRamRequired,
                OtherVRamRequired = otherVRamRequired,
                Unknown32 = unknown32,
                Unknown32_2 = unknown32_2
            };

            using (MemoryStream memory = new MemoryStream(rawData))
            {
                ushort pathLength = memory.ReadUInt16();
                script.Path = memory.ReadString(pathLength);
                uint scriptCount = memory.ReadUInt32();

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

                    script.Scripts.Add(scriptFile);
                }
            }

            return script;
        }

        public override byte[] Serialize()
        {
            using (MemoryStream memory = new MemoryStream())
            {
                memory.WriteUInt16((ushort)Path.Length);
                memory.WriteString(Path);
                memory.WriteUInt32((uint)Scripts.Count);
                memory.Write(Data, 0, Data.Length);
                return memory.ReadAllBytes();
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

        public override void ReplaceData(string path)
        {
            throw new NotSupportedException();
        }
    }

    public class ScriptFile : IResource
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
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(destination)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
            }

            System.IO.File.WriteAllBytes(destination, Data);
        }

        public byte[] Serialize()
        {
            using (MemoryStream memory = new MemoryStream())
            {
                memory.WriteUInt64(PathHash);
                memory.WriteUInt64(DataHash);
                memory.WriteUInt16((ushort)Path.Length);
                memory.WriteString(Path);
                memory.WriteUInt32((uint)Data.Length);
                memory.Write(Data, 0, Data.Length);
                return memory.ReadAllBytes();
            }
        }

        public void ReplaceData(string path)
        {
            throw new NotImplementedException();
        }
    }
}
