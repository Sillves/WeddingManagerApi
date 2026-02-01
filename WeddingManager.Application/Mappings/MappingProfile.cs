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
        CreateMap<Event, EventDto>()
            .ForMember(dest => dest.GuestDtos, opt => opt.MapFrom(src => src.Guests));
        CreateMap<CreateEventRequestDto, Event>();
        CreateMap<UpdateEventRequestDto, Event>();
        
        CreateMap<WeddingUser, WeddingUserDto>()
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"));

        // Expense mappings
        CreateMap<WeddingExpense, WeddingExpenseDto>();
        CreateMap<CreateWeddingExpenseRequestDto, WeddingExpense>();
        CreateMap<UpdateWeddingExpenseRequestDto, WeddingExpense>();

        // Website mappings
        CreateMap<WeddingWebsite, WeddingWebsiteDto>()
            .ForMember(dest => dest.WeddingSlug, opt => opt.MapFrom(src => src.Wedding.Slug))
            .ForMember(dest => dest.Settings, opt => opt.MapFrom(src => System.Text.Json.JsonDocument.Parse(src.Settings)))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => System.Text.Json.JsonDocument.Parse(src.Content)));

        // Media mappings
        CreateMap<Media, MediaUploadResponseDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.S3Url));
    }
}
