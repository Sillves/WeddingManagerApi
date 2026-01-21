using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;
using WeddingManager.Infrastructure.Repositories;

namespace WeddingManager.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WeddingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        services.AddIdentityConfiguration();
        services.AddScoped<IWeddingRepository, WeddingRepository>();

        return services;
    }
}
