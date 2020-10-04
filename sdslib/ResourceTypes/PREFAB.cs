using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class PREFAB : Resource
    {
        public new static PREFAB Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, ulong? nameHash, byte[] rawData, IMapper mapper)
        {
            PREFAB type = mapper.Map<PREFAB>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, nameHash, rawData, null));
            return type;
        }
    }
}
