using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WeddingManager.Infrastructure.Data;

/// <summary>
/// Design-time factory for WeddingDbContext.
/// Used by EF Core tools (migrations, database update) when running outside the application.
/// </summary>
public class WeddingDbContextFactory : IDesignTimeDbContextFactory<WeddingDbContext>
{
    public WeddingDbContext CreateDbContext(string[] args)
    {
        // Read connection string from environment variable
        var connectionString = Environment.GetEnvironmentVariable("DatabaseSettings__ConnectionString");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "DatabaseSettings__ConnectionString environment variable is not set. " +
                "This is required for EF Core design-time operations.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<WeddingDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new WeddingDbContext(optionsBuilder.Options);
    }
}
