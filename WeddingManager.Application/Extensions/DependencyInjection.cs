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
        services.AddScoped<ISubscriptionLimitService, SubscriptionLimitService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IWeddingExpenseService, WeddingExpenseService>();
        services.AddScoped<IWeddingBudgetService, WeddingBudgetService>();
        services.AddScoped<IWeddingWebsiteService, WeddingWebsiteService>();

        // Register Mapperly mapper as singleton (it's stateless)
        services.AddSingleton<ApplicationMapper>();

        return services;
    }
}
