using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;

namespace sdslib.ResourceTypes
{
    public class RoadMap : Resource
    {
        public new static RoadMap Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, uint? unknown32, uint? unknown32_2, byte[] rawData, IMapper mapper)
        {
            RoadMap type = mapper.Map<RoadMap>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, unknown32, unknown32_2, rawData, null));
            return type;
        }
    }
}
