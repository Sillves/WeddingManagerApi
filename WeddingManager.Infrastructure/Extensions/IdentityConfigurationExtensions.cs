using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using WeddingManager.Domain.Entities;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
    {
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<WeddingDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }
}
