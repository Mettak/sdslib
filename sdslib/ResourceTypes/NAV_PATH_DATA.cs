using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;

namespace sdslib.ResourceTypes
{
    public class NAV_PATH_DATA : Resource
    {
        public new static NAV_PATH_DATA Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, uint? unknown32, uint? unknown32_2, byte[] rawData, IMapper mapper)
        {
            NAV_PATH_DATA type = mapper.Map<NAV_PATH_DATA>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, unknown32, unknown32_2, rawData, null));
            return type;
        }
    }
}
