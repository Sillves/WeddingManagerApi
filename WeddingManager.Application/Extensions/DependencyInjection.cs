using Microsoft.Extensions.DependencyInjection;
using WeddingManager.Application.Mappings;
using WeddingManager.Application.Services;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add application services here as they are created
        services.AddScoped<IWeddingService, WeddingService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGuestService, GuestService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IWeddingUserService, WeddingUserService>();
        services.AddAutoMapper(cfg => 
        {
            cfg.AddProfile<MappingProfile>();
        });        
        return services;
    }
}
