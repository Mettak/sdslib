using sdslib.Enums;
using sdslib.ResourceTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace sdslib
{
    public class ResourceTypeList : List<ResourceType>
    {
        public new void Add(ResourceType item)
        {
            if (this.Any(x => x.Name == item.Name))
            {
                throw new Exception("Collection already contains this resource type");
            }

            if (this.Any(x => x.Id == item.Id))
            {
                throw new Exception("Index already used by another resource type");
            }

            base.Add(item);
        }

        public void Add(EResourceType item)
        {
            if (this.Any(x => x.Name == item))
            {
                throw new Exception("Collection already contains this resource type");
            }

            uint index = 0;
            if (Count > 0)
            {
                index = this.Last().Id + 1;
            }

            base.Add(new ResourceType
            {
                Id = index,
                Name = item
            });
        }
    }
}
