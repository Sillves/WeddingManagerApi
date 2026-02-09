using Microsoft.Extensions.Options;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Application.Services;

public class SubscriptionLimitService(
    IWeddingRepository weddingRepository,
    IGuestRepository guestRepository,
    IEventRepository eventRepository,
    ISubscriptionUsageRepository subscriptionUsageRepository,
    IOptions<SubscriptionPlanOptions> planOptions)
    : ISubscriptionLimitService
{
    public async Task<Result> EnsureGuestLimitAsync(Guid weddingId)
    {
        var weddingResult = await GetWeddingAsync(weddingId);
        if (!weddingResult.IsSuccess)
        {
            return Result.Fail(weddingResult.Errors);
        }

        var limitsResult = GetLimits(weddingResult.Value!);
        if (!limitsResult.IsSuccess)
        {
            return Result.Fail(limitsResult.Errors);
        }

        var limits = limitsResult.Value!;

        if (limits.MaxGuests < 0)
        {
            return Result.Ok();
        }

        var count = await guestRepository.CountByWeddingIdAsync(weddingId);
        if (count >= limits.MaxGuests)
        {
            return Result.Fail(new Error(
                ErrorCodes.LimitExceeded,
                $"Guest limit reached for the {weddingResult.Value!.User.SubscriptionTier} plan."));
        }

        return Result.Ok();
    }

    public async Task<Result> EnsureGuestLimitAsync(Guid weddingId, int count)
    {
        if (count <= 0)
        {
            return Result.Ok();
        }

        var weddingResult = await GetWeddingAsync(weddingId);
        if (!weddingResult.IsSuccess)
        {
            return Result.Fail(weddingResult.Errors);
        }

        var limitsResult = GetLimits(weddingResult.Value!);
        if (!limitsResult.IsSuccess)
        {
            return Result.Fail(limitsResult.Errors);
        }

        var limits = limitsResult.Value!;

        if (limits.MaxGuests < 0)
        {
            return Result.Ok();
        }

        var currentCount = await guestRepository.CountByWeddingIdAsync(weddingId);
        if (currentCount + count > limits.MaxGuests)
        {
            return Result.Fail(new Error(
                ErrorCodes.LimitExceeded,
                $"Adding {count} guests would exceed the guest limit for the {weddingResult.Value!.User.SubscriptionTier} plan."));
        }

        return Result.Ok();
    }

    public async Task<Result> EnsureEventLimitAsync(Guid weddingId)
    {
        var weddingResult = await GetWeddingAsync(weddingId);
        if (!weddingResult.IsSuccess)
        {
            return Result.Fail(weddingResult.Errors);
        }

        var limitsResult = GetLimits(weddingResult.Value!);
        if (!limitsResult.IsSuccess)
        {
            return Result.Fail(limitsResult.Errors);
        }

        var limits = limitsResult.Value!;

        if (limits.MaxEvents < 0)
        {
            return Result.Ok();
        }

        var count = await eventRepository.CountByWeddingIdAsync(weddingId);
        if (count >= limits.MaxEvents)
        {
            return Result.Fail(new Error(
                ErrorCodes.LimitExceeded,
                $"Event limit reached for the {weddingResult.Value!.User.SubscriptionTier} plan."));
        }

        return Result.Ok();
    }

    public async Task<Result> EnsureEmailLimitAsync(Guid weddingId, int emailCount)
    {
        if (emailCount <= 0)
        {
            return Result.Ok();
        }

        var weddingResult = await GetWeddingAsync(weddingId);
        if (!weddingResult.IsSuccess)
        {
            return Result.Fail(weddingResult.Errors);
        }

        var limitsResult = GetLimits(weddingResult.Value!);
        if (!limitsResult.IsSuccess)
        {
            return Result.Fail(limitsResult.Errors);
        }

        var limits = limitsResult.Value!;

        if (limits.MaxEmailsPerMonth < 0)
        {
            return Result.Ok();
        }

        var (year, month) = GetPeriod();
        var usage = await subscriptionUsageRepository.GetByPeriodAsync(weddingResult.Value!.UserId, year, month);
        var current = usage?.EmailsSent ?? 0;

        if (current + emailCount > limits.MaxEmailsPerMonth)
        {
            return Result.Fail(new Error(
                ErrorCodes.LimitExceeded,
                $"Monthly email limit reached for the {weddingResult.Value!.User.SubscriptionTier} plan."));
        }

        return Result.Ok();
    }

    public async Task<Result> RecordEmailsSentAsync(Guid userId, int emailCount)
    {
        if (emailCount <= 0)
        {
            return Result.Ok();
        }

        var (year, month) = GetPeriod();
        var usage = await subscriptionUsageRepository.GetByPeriodAsync(userId, year, month);

        if (usage == null)
        {
            usage = new SubscriptionUsage
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Year = year,
                Month = month,
                EmailsSent = emailCount
            };
            await subscriptionUsageRepository.AddAsync(usage);
            return Result.Ok();
        }

        usage.EmailsSent += emailCount;
        await subscriptionUsageRepository.UpdateAsync(usage);
        return Result.Ok();
    }

    private async Task<Result<Wedding>> GetWeddingAsync(Guid weddingId)
    {
        var wedding = await weddingRepository.GetByIdAsync(weddingId);
        return wedding == null
            ? Result<Wedding>.Fail(new Error(ErrorCodes.NotFound, "Wedding not found"))
            : Result<Wedding>.Ok(wedding);
    }

    private Result<SubscriptionPlanLimits> GetLimits(Wedding wedding)
    {
        try
        {
            return Result<SubscriptionPlanLimits>.Ok(planOptions.Value.GetLimits(wedding.User.SubscriptionTier));
        }
        catch (InvalidOperationException ex)
        {
            return Result<SubscriptionPlanLimits>.Fail(
                new Error(ErrorCodes.Unexpected, ex.Message));
        }
    }

    private static (int Year, int Month) GetPeriod()
    {
        var now = DateTime.UtcNow;
        return (now.Year, now.Month);
    }
}
