using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.Models
{
    public class ResourceInfo
    {
        public string SourceDataDescription { get; set; }

        public string Name
        {
            get
            {
                if (SourceDataDescription == "not available")
                {
                    return Guid.NewGuid().ToString();
                }

                return SourceDataDescription;
            }
        }

        public ResourceType Type { get; set; }
    }
}
