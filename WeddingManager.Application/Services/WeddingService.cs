using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class WeddingService(
    IWeddingRepository repository,
    IUserContextService userContextService,
    IWeddingUserRepository weddingUserRepository) : IWeddingService
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

    public async Task<Result<IEnumerable<WeddingWithRoleDto>>> GetAllWithRoleAsync()
    {
        var userId = userContextService.GetUserId();
        var results = await repository.GetAllWithRoleAsync(userId);
        var dtos = results.Select(r => new WeddingWithRoleDto
        {
            Id = r.Wedding.Id,
            Title = r.Wedding.Title,
            Slug = r.Wedding.Slug,
            Date = r.Wedding.Date,
            Location = r.Wedding.Location,
            Role = r.WeddingUser.Role,
            GuestCount = r.Wedding.Guests?.Count ?? 0,
            CanAccessGuests = r.WeddingUser.CanAccessGuests,
            CanAccessEvents = r.WeddingUser.CanAccessEvents,
            CanAccessExpenses = r.WeddingUser.CanAccessExpenses,
            CanAccessWebsite = r.WeddingUser.CanAccessWebsite,
            IsReadOnly = r.WeddingUser.IsReadOnly,
        });
        return Result<IEnumerable<WeddingWithRoleDto>>.Ok(dtos);
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

        var ownerWeddingUser = new WeddingUser
        {
            WeddingId = wedding.Id,
            UserId = wedding.UserId,
            Role = WeddingUserRole.Owner,
            AddedAt = DateTime.UtcNow,
            CanAccessGuests = true,
            CanAccessEvents = true,
            CanAccessExpenses = true,
            CanAccessWebsite = true,
            IsReadOnly = false,
        };
        await weddingUserRepository.AddAsync(ownerWeddingUser);

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
