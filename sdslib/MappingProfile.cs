using AutoMapper;
using sdslib.ResourceTypes;
using System;

namespace sdslib
{
    public partial class SdsFile : IDisposable
    {
        private class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<Resource, Actors>();
                CreateMap<Resource, AnimalTrafficPaths>();
                CreateMap<Resource, AnimatedTexture>();
                CreateMap<Resource, Animation2>();
                CreateMap<Resource, AudioSectors>();
                CreateMap<Resource, Collisions>();
                CreateMap<Resource, Cutscene>();
                CreateMap<Resource, Effects>();
                CreateMap<Resource, EntityDataStorage>();
                CreateMap<Resource, FrameNameTable>();
                CreateMap<Resource, FrameResource>();
                CreateMap<Resource, FxActor>();
                CreateMap<Resource, FxAnimSet>();
                CreateMap<Resource, IndexBufferPool>();
                CreateMap<Resource, ItemDesc>();
                CreateMap<Resource, MemFile>();
                CreateMap<Resource, Mipmap>();
                CreateMap<Resource, NAV_AIWORLD_DATA>();
                CreateMap<Resource, NAV_OBJ_DATA>();
                CreateMap<Resource, PREFAB>();
                CreateMap<Resource, Script>()
                    .ForMember(m => m.Data, dest => dest.Ignore());
                CreateMap<Resource, Sound>();
                CreateMap<Resource, SoundTable>();
                CreateMap<Resource, Speech>();
                CreateMap<Resource, Table>();
                CreateMap<Resource, Texture>();
                CreateMap<Resource, Translokator>();
                CreateMap<Resource, VertexBufferPool>();
                CreateMap<Resource, XML>()
                    .ForMember(m => m.Data, dest => dest.Ignore());
                CreateMap<Resource, Flash>();
                CreateMap<Resource, SystemObjectDatabase>();
                CreateMap<Resource, Generic>();
                CreateMap<Resource, hkAnimation>();
                CreateMap<Resource, NAV_PATH_DATA>();
                CreateMap<Resource, RoadMap>();
            }
        }
    }
}
