using AutoMapper;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Wedding mappings
        CreateMap<Wedding, WeddingDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId));
        
        CreateMap<CreateWeddingRequestDto, Wedding>();
        
    }
}