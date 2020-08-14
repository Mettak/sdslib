using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class Resource
    {
        public string Guid { get; set; }

        public ResourceInfo Info { get; set; } = new ResourceInfo();

        [JsonIgnore]
        public string Name
        {
            get
            {
                if (Info.SourceDataDescription == "not available")
                {
                    return Guid;
                }

                return Info.SourceDataDescription;
            }
        }

        [JsonIgnore]
        public virtual uint Size
        {
            get
            {
                return Constants.Resource.StandardHeaderSize + (uint)Data.Length;
            }
        }

        public ushort Version { get; set; }

        public uint SlotRamRequired { get; set; }

        public uint SlotVRamRequired { get; set; }

        public uint OtherRamRequired { get; set; }

        public uint OtherVRamRequired { get; set; }

        [JsonIgnore]
        public uint Checksum
        {
            get
            {
                byte[] bytes = new byte[26];
                Array.Copy(BitConverter.GetBytes(Info.Type.Id), 0, bytes, 0, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Size), 0, bytes, 4, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(Version), 0, bytes, 8, Constants.DataTypesSizes.UInt16);
                Array.Copy(BitConverter.GetBytes(SlotRamRequired), 0, bytes, 10, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(SlotVRamRequired), 0, bytes, 14, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(OtherRamRequired), 0, bytes, 18, Constants.DataTypesSizes.UInt32);
                Array.Copy(BitConverter.GetBytes(OtherVRamRequired), 0, bytes, 22, Constants.DataTypesSizes.UInt32);
                return FNV.Hash32(bytes);
            }
        }

        [JsonIgnore]
        public virtual byte[] Data { get; set; }

        [JsonConstructor]
        public Resource() { }

        public Resource(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, byte[] rawData)
        {
            Guid = System.Guid.NewGuid().ToString();
            Info = resourceInfo;
            Version = version;
            SlotRamRequired = slotRamRequired;
            SlotVRamRequired = slotVRamRequired;
            OtherRamRequired = otherRamRequired;
            OtherVRamRequired = otherVRamRequired;
            Data = rawData;
        }

        public virtual byte[] GetRawData()
        {
            return Data;
        }

        public virtual void Extract(string destination)
        {
            if (!Directory.Exists(Path.GetDirectoryName(destination)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
            }

            System.IO.File.WriteAllBytes(destination, Data);
        }

        public virtual void ReplaceData(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            Data = System.IO.File.ReadAllBytes(path);
        }
    }
}
