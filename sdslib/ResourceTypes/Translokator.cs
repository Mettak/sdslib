using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class Translokator : Resource
    {
        public new static Translokator Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, uint? unknown32, uint? unknown32_2, byte[] rawData, IMapper mapper)
        {
            Translokator type = mapper.Map<Translokator>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, unknown32, unknown32_2, rawData, null));
            return type;
        }
    }
}
