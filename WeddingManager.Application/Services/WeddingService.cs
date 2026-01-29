using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class WeddingService(IWeddingRepository repository, IUserContextService userContextService) : IWeddingService
{
    public async Task<Result<Wedding>> GetByIdAsync(Guid id)
    {
        var wedding = await repository.GetByIdAsync(id);
        return wedding == null
            ? Result<Wedding>.Fail(new Error(ErrorCodes.NotFound, $"Wedding with id {id} not found"))
            : Result<Wedding>.Ok(wedding);
    }

    public async Task<Result<Wedding>> GetByIdOrSlugAsync(string idOrSlug)
    {
        var wedding = await repository.GetByIdOrSlugAsync(idOrSlug);
        return wedding == null
            ? Result<Wedding>.Fail(new Error(ErrorCodes.NotFound, $"Wedding {idOrSlug} not found"))
            : Result<Wedding>.Ok(wedding);
    }

    public async Task<Result> UpdateAsync(Wedding wedding)
    {
        await repository.UpdateAsync(wedding);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        await repository.DeleteAsync(id);
        return Result.Ok();
    }
    
    public async Task<Result<IEnumerable<Wedding>>> GetAllAsync()
    {
        var userId = userContextService.GetUserId();
        var weddings = await repository.GetAllAsync(userId);
        return Result<IEnumerable<Wedding>>.Ok(weddings);
    }

    public async Task<Result> AddAsync(Wedding wedding)
    {
        wedding.Id = Guid.NewGuid();
        if (wedding.UserId == Guid.Empty)
        {
            wedding.UserId = userContextService.GetUserId();
        }
        
        wedding.Slug = GenerateSlug(wedding.Title);
        
        await repository.AddAsync(wedding);
        return Result.Ok();
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
