using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data;

public class WeddingDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public WeddingDbContext(DbContextOptions<WeddingDbContext> options) : base(options)
    {
    }

    public DbSet<Wedding> Weddings => Set<Wedding>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Media> Media => Set<Media>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
