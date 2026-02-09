using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Utils;
using WeddingManager.Infrastructure.Data;
using WeddingManager.Infrastructure.Repositories;
using WeddingManager.Infrastructure.Services;

namespace WeddingManager.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Inject IOptions<DatabaseSettings> in DbContext
        services.AddDbContext<WeddingDbContext>((serviceProvider, options) =>
        {
            var dbSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>();
            options.UseNpgsql(dbSettings.Value.ConnectionString);
        });

        services.AddHttpContextAccessor();
        
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddIdentityConfiguration();
        
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();
        
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        services.AddScoped<IWeddingRepository, WeddingRepository>();
        services.AddScoped<IGuestRepository, GuestRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IWeddingUserRepository, WeddingUserRepository>();
        services.AddScoped<ISubscriptionUsageRepository, SubscriptionUsageRepository>();
        services.AddScoped<IWeddingExpenseRepository, WeddingExpenseRepository>();
        services.AddScoped<IWeddingBudgetRepository, WeddingBudgetRepository>();
        services.AddScoped<IWeddingWebsiteRepository, WeddingWebsiteRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IMediaService, MediaService>();

        // Use local file storage if Scaleway is not configured, otherwise use Scaleway
        var scalewaySettings = configuration.GetSection("ScalewayStorage").Get<ScalewayStorageSettings>();
        if (string.IsNullOrEmpty(scalewaySettings?.AccessKey))
        {
            services.AddScoped<IObjectStorageService, LocalFileStorageService>();
        }
        else
        {
            services.AddScoped<IObjectStorageService, ScalewayObjectStorageService>();
        }

        // Register background service for media cleanup (runs daily, deletes media 2 weeks after wedding)
        services.AddHostedService<MediaCleanupService>();

        return services;
    }
}
