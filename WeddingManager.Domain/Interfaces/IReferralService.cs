using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IReferralService
{
    Task<Result<string>> GetOrCreateReferralCodeAsync(Guid userId);
    Task<Result<ReferralStatsDto>> GetStatsAsync(Guid userId);
    Task<Result<IEnumerable<ReferralDto>>> GetReferralsAsync(Guid userId);
    Task<Result> TrackReferralRegistrationAsync(string referralCode, Guid newUserId);
    Task<Result> TrackReferralConversionAsync(Guid userId);
}
