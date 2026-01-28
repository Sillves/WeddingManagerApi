using Microsoft.Extensions.Options;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Exceptions;
using WeddingManager.Domain.Interfaces;
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
    public async Task EnsureGuestLimitAsync(Guid weddingId)
    {
        var wedding = await GetWeddingAsync(weddingId);
        var limits = planOptions.Value.GetLimits(wedding.User.SubscriptionTier);

        if (limits.MaxGuests < 0)
        {
            return;
        }

        var count = await guestRepository.CountByWeddingIdAsync(weddingId);
        if (count >= limits.MaxGuests)
        {
            throw new SubscriptionLimitExceededException(
                $"Guest limit reached for the {wedding.User.SubscriptionTier} plan.");
        }
    }

    public async Task EnsureEventLimitAsync(Guid weddingId)
    {
        var wedding = await GetWeddingAsync(weddingId);
        var limits = planOptions.Value.GetLimits(wedding.User.SubscriptionTier);

        if (limits.MaxEvents < 0)
        {
            return;
        }

        var count = await eventRepository.CountByWeddingIdAsync(weddingId);
        if (count >= limits.MaxEvents)
        {
            throw new SubscriptionLimitExceededException(
                $"Event limit reached for the {wedding.User.SubscriptionTier} plan.");
        }
    }

    public async Task EnsureEmailLimitAsync(Guid weddingId, int emailCount)
    {
        if (emailCount <= 0)
        {
            return;
        }

        var wedding = await GetWeddingAsync(weddingId);
        var limits = planOptions.Value.GetLimits(wedding.User.SubscriptionTier);

        if (limits.MaxEmailsPerMonth < 0)
        {
            return;
        }

        var (year, month) = GetPeriod();
        var usage = await subscriptionUsageRepository.GetByPeriodAsync(wedding.UserId, year, month);
        var current = usage?.EmailsSent ?? 0;

        if (current + emailCount > limits.MaxEmailsPerMonth)
        {
            throw new SubscriptionLimitExceededException(
                $"Monthly email limit reached for the {wedding.User.SubscriptionTier} plan.");
        }
    }

    public async Task RecordEmailsSentAsync(Guid userId, int emailCount)
    {
        if (emailCount <= 0)
        {
            return;
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
            return;
        }

        usage.EmailsSent += emailCount;
        await subscriptionUsageRepository.UpdateAsync(usage);
    }

    private async Task<Wedding> GetWeddingAsync(Guid weddingId)
    {
        return await weddingRepository.GetByIdAsync(weddingId)
               ?? throw new KeyNotFoundException("Wedding not found");
    }

    private static (int Year, int Month) GetPeriod()
    {
        var now = DateTime.UtcNow;
        return (now.Year, now.Month);
    }
}
