using AutoMapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class SystemObjectDatabase : Resource
    {
        public override Dictionary<uint, List<ushort>> SupportedVersions =>
            new Dictionary<uint, List<ushort>>()
            {
                {
                    20U,
                    new List<ushort>()
                    {
                        1
                    }
                },
            };

        public new static SystemObjectDatabase Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, ulong? nameHash, byte[] rawData, IMapper mapper)
        {
            SystemObjectDatabase type = mapper.Map<SystemObjectDatabase>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, nameHash, rawData, null));
            return type;
        }
    }
}
