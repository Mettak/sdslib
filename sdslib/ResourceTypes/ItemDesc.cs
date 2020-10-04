using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class ItemDesc : Resource
    {
        public new static ItemDesc Deserialize(ResourceInfo resourceInfo, ushort version, uint slotRamRequired, uint slotVRamRequired, uint otherRamRequired, uint otherVRamRequired, ulong? nameHash, byte[] rawData, IMapper mapper)
        {
            ItemDesc type = mapper.Map<ItemDesc>(Resource.Deserialize(resourceInfo, version, slotRamRequired, slotVRamRequired, otherRamRequired, otherVRamRequired, nameHash, rawData, null));
            return type;
        }
    }
}
