using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.Models
{
    public class ResourceType
    {
        public uint Id { get; set; }

        public uint Unknown32 { get; set; }

        public EResourceType Type { get; set; }
    }
}
