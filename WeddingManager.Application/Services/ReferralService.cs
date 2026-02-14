using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class ReferralService(
    IReferralRepository referralRepository,
    UserManager<User> userManager,
    ILogger<ReferralService> logger) : IReferralService
{
    public async Task<Result<string>> GetOrCreateReferralCodeAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result<string>.Fail(new Error(ErrorCodes.NotFound, "User not found"));

        if (!string.IsNullOrEmpty(user.ReferralCode))
            return Result<string>.Ok(user.ReferralCode);

        var code = GenerateReferralCode(user);
        user.ReferralCode = code;
        await userManager.UpdateAsync(user);

        return Result<string>.Ok(code);
    }

    public async Task<Result<ReferralStatsDto>> GetStatsAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result<ReferralStatsDto>.Fail(new Error(ErrorCodes.NotFound, "User not found"));

        var referrals = (await referralRepository.GetByReferrerUserIdAsync(userId)).ToList();

        var signedUp = referrals.Count(r => r.RegisteredAt != null);
        var subscribed = referrals.Count(r => r.ConvertedAt != null);

        return Result<ReferralStatsDto>.Ok(new ReferralStatsDto
        {
            ReferralCode = user.ReferralCode ?? string.Empty,
            SignedUpCount = signedUp,
            SubscribedCount = subscribed,
            ConversionRate = signedUp > 0 ? Math.Round((decimal)subscribed / signedUp * 100, 1) : 0,
            TotalCommissionEarned = referrals
                .Where(r => r.CommissionStatus == CommissionStatus.Paid)
                .Sum(r => r.CommissionPercentage),
            PendingPayout = referrals
                .Where(r => r.CommissionStatus == CommissionStatus.Pending && r.ConvertedAt != null)
                .Sum(r => r.CommissionPercentage),
        });
    }

    public async Task<Result<IEnumerable<ReferralDto>>> GetReferralsAsync(Guid userId)
    {
        var referrals = await referralRepository.GetByReferrerUserIdAsync(userId);

        var dtos = referrals.Select(r => new ReferralDto
        {
            Id = r.Id,
            ReferredUserName = r.ReferredUser != null
                ? $"{r.ReferredUser.FirstName} {r.ReferredUser.LastName}".Trim()
                : null,
            RegisteredAt = r.RegisteredAt,
            ConvertedAt = r.ConvertedAt,
            CommissionPercentage = r.CommissionPercentage,
            CommissionStatus = r.CommissionStatus,
            CreatedAt = r.CreatedAt,
        });

        return Result<IEnumerable<ReferralDto>>.Ok(dtos);
    }

    public async Task<Result> TrackReferralRegistrationAsync(string referralCode, Guid newUserId)
    {
        var referrer = userManager.Users.FirstOrDefault(u => u.ReferralCode == referralCode);
        if (referrer == null)
        {
            logger.LogWarning("Referral code {Code} not found", referralCode);
            return Result.Ok(); // silently ignore invalid codes
        }

        // Don't let users refer themselves
        if (referrer.Id == newUserId)
            return Result.Ok();

        // Check if this user was already referred
        var existing = await referralRepository.GetByReferredUserIdAsync(newUserId);
        if (existing != null)
            return Result.Ok();

        var referral = new Referral
        {
            Id = Guid.NewGuid(),
            ReferrerUserId = referrer.Id,
            ReferredUserId = newUserId,
            ReferralCode = referralCode,
            RegisteredAt = DateTime.UtcNow,
            CommissionPercentage = 15.0m,
            CommissionStatus = CommissionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        await referralRepository.AddAsync(referral);
        logger.LogInformation("Tracked referral registration: {Code} -> {UserId}", referralCode, newUserId);

        return Result.Ok();
    }

    public async Task<Result> TrackReferralConversionAsync(Guid userId)
    {
        var referral = await referralRepository.GetByReferredUserIdAsync(userId);
        if (referral == null || referral.ConvertedAt != null)
            return Result.Ok(); // no referral or already converted

        referral.ConvertedAt = DateTime.UtcNow;
        await referralRepository.UpdateAsync(referral);
        logger.LogInformation("Tracked referral conversion for user {UserId}", userId);

        return Result.Ok();
    }

    internal static string GenerateReferralCode(User user)
    {
        var name = $"{user.FirstName}-{user.LastName}".ToLowerInvariant();
        // Remove diacritics
        var normalized = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        // Replace non-alphanumeric (except hyphens) with hyphens, collapse multiple hyphens
        var slug = Regex.Replace(sb.ToString(), @"[^a-z0-9-]", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');

        // Add short random suffix to ensure uniqueness
        var suffix = Guid.NewGuid().ToString("N")[..6];
        return $"{slug}-{suffix}";
    }
}
