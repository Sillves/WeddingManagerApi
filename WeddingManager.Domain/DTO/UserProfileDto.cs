using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public record UserProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    SubscriptionTier SubscriptionTier,
    AccountType AccountType);
