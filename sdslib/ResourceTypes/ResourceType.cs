using Newtonsoft.Json;
using sdslib.Enums;

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

                else if (Name == EResourceType.NAV_PATH_DATA)
                {
                    return 1U;
                }

                else
                {
                    return 0U;
                }
            }
        }

        public EResourceType Name { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
