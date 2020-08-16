using AutoMapper;
using sdslib.ResourceTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Resource, Texture>();
            CreateMap<Resource, MipMap>();
        }
    }
}
