using Microsoft.Extensions.DependencyInjection;
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
        return services;
    }
}
