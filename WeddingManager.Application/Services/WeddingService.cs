using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Application.Services;

public class WeddingService(IWeddingRepository repository, IUserContextService userContextService) : IWeddingService
{
    public async Task<Wedding?> GetByIdAsync(Guid id) => await repository.GetByIdAsync(id);
    public async Task<Wedding?> GetByIdOrSlugAsync(string idOrSlug) => await repository.GetByIdOrSlugAsync(idOrSlug);
    public async Task UpdateAsync(Wedding wedding) => await repository.UpdateAsync(wedding);
    public async Task DeleteAsync(Guid id) => await repository.DeleteAsync(id);
    
    public async Task<IEnumerable<Wedding>> GetAllAsync()
    {
        var userId = userContextService.GetUserId();
        return await repository.GetAllAsync(userId);
    }

    public async Task AddAsync(Wedding wedding)
    {
        wedding.Id = Guid.NewGuid();
        if (wedding.UserId == Guid.Empty)
        {
            wedding.UserId = userContextService.GetUserId();
        }
        
        wedding.Slug = GenerateSlug(wedding.Title);
        
        await repository.AddAsync(wedding);
    }
    
    private static string GenerateSlug(string title)
    {
        // Lowercase
        var slug = title.ToLower();
    
        // Remove special characters, keep only alphanumeric and spaces
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
    
        // Replace spaces with hyphens
        slug = slug.Replace(" ", "-");
    
        // Remove multiple hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
    
        // Trim hyphens from start/end
        slug = slug.Trim('-');
    
        return slug;
    }
}
