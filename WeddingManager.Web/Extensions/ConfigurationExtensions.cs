using LodeKennes.Extensions.Scaleway.SecretManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Web.Extensions;

public static class ConfigurationExtensions
{
    public static IConfiguration ConfigureAppConfiguration(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddScalewayCliSecrets(options =>
            {
                options.ProjectId = Guid.Parse(Environment.GetEnvironmentVariable("SCW_DEFAULT_PROJECT_ID"));
                options.EnableCaching(TimeSpan.FromMinutes(15));
                options.UseCli();
            });
        }

        return builder.Configuration;
    }

    public static IServiceCollection AddDatabaseSettings(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        if (isDevelopment)
        {
            services.Configure<DatabaseSettings>(configuration.GetSection("DatabaseSettings"));
        }
        else
        {
            services.Configure<DatabaseSettings>(options =>
            {
                options.ConnectionString = configuration["DatabaseSettings__ConnectionString"];
            });
        }

        services.PostConfigure<DatabaseSettings>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                throw new InvalidOperationException("No database connection string provided");
            }
        });

        return services;
    }
}
