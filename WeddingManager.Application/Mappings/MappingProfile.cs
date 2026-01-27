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
        CreateMap<Wedding, WeddingPublicDto>();
        CreateMap<CreateWeddingRequestDto, Wedding>();
        
        // Guest mappings
        CreateMap<Guest, GuestDto>();
        CreateMap<CreateGuestRequestDto, Guest>();
        CreateMap<UpdateGuestRequestDto, Guest>();

        // Event mappings
        CreateMap<Event, EventDto>();
        CreateMap<CreateEventRequestDto, Event>();
        CreateMap<UpdateEventRequestDto, Event>();
        
        CreateMap<WeddingUser, WeddingUserDto>()
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"));
        
    }
}
