using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data;

public class WeddingDbContext(DbContextOptions<WeddingDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Wedding> Weddings => Set<Wedding>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<WeddingUser> WeddingUsers => Set<WeddingUser>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<SubscriptionUsage> SubscriptionUsages => Set<SubscriptionUsage>();
    public DbSet<WeddingExpense> WeddingExpenses => Set<WeddingExpense>();
    public DbSet<WeddingWebsite> WeddingWebsites => Set<WeddingWebsite>();
    public DbSet<WeddingBudget> WeddingBudgets => Set<WeddingBudget>();
    public DbSet<BudgetAllocation> BudgetAllocations => Set<BudgetAllocation>();
    public DbSet<WeddingInvitation> WeddingInvitations => Set<WeddingInvitation>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<ReferralPayout> ReferralPayouts => Set<ReferralPayout>();
    public DbSet<InvitationFlow> InvitationFlows => Set<InvitationFlow>();
    public DbSet<RsvpResponse> RsvpResponses => Set<RsvpResponse>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
