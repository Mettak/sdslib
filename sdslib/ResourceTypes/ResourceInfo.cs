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
        public string SourceDataDescription { get; set; } = "not available";

        [JsonIgnore]
        public ResourceType Type { get; set; }
    }
}
