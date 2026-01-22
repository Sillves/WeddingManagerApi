using System.Reflection;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Application.Services;

public class WeddingService : IWeddingService
{
    private readonly IWeddingRepository _repository;
    private readonly IUserContextService _userContextService;

    public WeddingService(IWeddingRepository repository, IUserContextService userContextService)
    {
        _repository = repository;
        _userContextService = userContextService;
    }
    
    public async Task<Wedding?> GetByIdAsync(Guid id) => await _repository.GetByIdAsync(id);
    public async Task UpdateAsync(Wedding wedding) => await _repository.UpdateAsync(wedding);
    public async Task DeleteAsync(Guid id) => await _repository.DeleteAsync(id);
    
    public async Task<IEnumerable<Wedding>> GetAllAsync()
    {
        var userId = _userContextService.GetUserId();
        return await _repository.GetAllAsync(userId);
    }

    public async Task AddAsync(Wedding wedding)
    {
        wedding.Id = Guid.NewGuid();
        if (wedding.UserId == Guid.Empty)
        {
            wedding.UserId = _userContextService.GetUserId();
        }
        
        wedding.Slug = GenerateSlug(wedding.Title);
        
        await _repository.AddAsync(wedding);
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