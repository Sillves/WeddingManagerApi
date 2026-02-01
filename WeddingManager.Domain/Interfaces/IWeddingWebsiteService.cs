using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingWebsiteService
{
    Task<Result<WeddingWebsiteDto>> GetByWeddingIdAsync(Guid weddingId);
    Task<Result<WeddingWebsiteDto>> CreateAsync(Guid userId, Guid weddingId, CreateWeddingWebsiteRequestDto request);
    Task<Result<WeddingWebsiteDto>> UpdateAsync(Guid weddingId, UpdateWeddingWebsiteRequestDto request);
    Task<Result<WeddingWebsiteDto>> PublishAsync(Guid weddingId);
    Task<Result<WeddingWebsiteDto>> UnpublishAsync(Guid weddingId);
    Task<Result<PublicWeddingWebsiteDto>> GetPublicBySlugAsync(string slug);
    Task<Result> DeleteAsync(Guid weddingId);
}
