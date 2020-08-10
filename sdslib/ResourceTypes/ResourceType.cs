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

        public uint Unknown32 { get; set; }

        public EResourceType Name { get; set; }

        public string DisplayName
        {
            get
            {
                return Name.ToString();
            }
        }
    }
}
