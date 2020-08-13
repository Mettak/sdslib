using Newtonsoft.Json;
using sdslib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class ResourceType
    {
        public uint Id { get; set; }

        [JsonIgnore]
        public uint Unknown32
        {
            get
            {
                if (Name == EResourceType.IndexBufferPool || Name == EResourceType.PREFAB)
                {
                    return 3U;
                }

                else if (Name == EResourceType.VertexBufferPool || Name == EResourceType.NAV_OBJ_DATA)
                {
                    return 2U;
                }

                else
                {
                    return 0U;
                }
            }
        }

        public EResourceType Name { get; set; }

        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                return Name.ToString();
            }
        }
    }
}
