namespace WeddingManager.Domain.Entities;

public class Wedding
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public ICollection<WeddingUser> WeddingUsers { get; set; } = null!;
    public ICollection<Guest> Guests { get; set; } = null!;
    public ICollection<Page> Pages { get; set; } = null!;
    public ICollection<Media> Media { get; set; } = null!;
    public ICollection<Event> Events { get; set; } = null!;
    public ICollection<WeddingExpense> Expenses { get; set; } = null!;
    public WeddingWebsite? Website { get; set; }
    public WeddingBudget? Budget { get; set; }
}
