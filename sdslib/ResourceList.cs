using sdslib.ResourceTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib
{
    public class ResourceList<T> : List<T> where T : Resource
    {
        public new void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Add(T resource, List<ResourceType> resourceTypes)
        {
            if (resourceTypes == null)
            {
                throw new ArgumentNullException(nameof(resourceTypes));
            }

            if (resource.GetType().BaseType != typeof(Resource))
            {
                throw new Exception("Cannot add base type with this function.");
            }

            string typeName = resource.GetType().Name;

            var type = resourceTypes.FirstOrDefault(x => x.ToString() == typeName);
            resource.Info.Type = type ?? throw new Exception($"Resource type {typeName} not found");
            base.Add(resource);
        }

        public List<MemoryStream> MergeDataIntoBlocks(int blockSize)
        {
            MemoryStream mergedData = new MemoryStream();
            foreach (var resource in this)
            {
                mergedData.WriteUInt32(resource.Info.Type.Id);
                mergedData.WriteUInt32(resource.Size);
                mergedData.WriteUInt16(resource.Version);

                if (resource.ResourceNameHash.HasValue)
                {
                    mergedData.WriteUInt64(resource.ResourceNameHash ??
                        throw new NullReferenceException(nameof(resource.ResourceNameHash)));
                }

                mergedData.WriteUInt32(resource.SlotRamRequired);
                mergedData.WriteUInt32(resource.SlotVRamRequired);
                mergedData.WriteUInt32(resource.OtherRamRequired);
                mergedData.WriteUInt32(resource.OtherVRamRequired);
                mergedData.WriteUInt32(resource.Checksum);
                mergedData.Write(resource.Serialize());
            }

            mergedData.SeekToStart();

            int numberOfBlocks = (int)mergedData.Length / blockSize;

            List<MemoryStream> dataBlocks = new List<MemoryStream>();
            for (int i = 0; i < numberOfBlocks; i++)
            {
                MemoryStream dataBlock = new MemoryStream();
                dataBlock.Write(mergedData.ReadBytes(blockSize));
                dataBlocks.Add(dataBlock);
            }

            if (mergedData.Position != mergedData.Length)
            {
                MemoryStream dataBlock = new MemoryStream();
                dataBlock.Write(mergedData.ReadBytes((int)mergedData.Length - (int)mergedData.Position));
                dataBlocks.Add(dataBlock);
            }

            mergedData.Close();
            return dataBlocks;
        }
    }
}
