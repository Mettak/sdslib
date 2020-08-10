using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ResourceTypes
{
    public class ResourceInfo
    {
        public string SourceDataDescription { get; set; }

        public string ResourceGuid { get; set; }

        [JsonIgnore]
        public string Name
        {
            get
            {
                if (SourceDataDescription == "not available")
                {
                    return ResourceGuid;
                }

                return SourceDataDescription;
            }
        }

        public ResourceType Type { get; set; }

        public ResourceInfo()
        {
            ResourceGuid = Guid.NewGuid().ToString();
        }
    }
}
