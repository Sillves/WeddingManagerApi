using Microsoft.AspNetCore.Identity;

namespace WeddingManager.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Wedding> Weddings { get; set; } = null!;
    public ICollection<WeddingUser> WeddingUsers { get; set; } = null!;
}