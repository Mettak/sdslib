using Newtonsoft.Json;

namespace sdslib.ResourceTypes
{
    public class ResourceInfo
    {
        public string SourceDataDescription { get; set; } = "not available";

        [JsonIgnore]
        public ResourceType Type { get; set; }
    }
}
