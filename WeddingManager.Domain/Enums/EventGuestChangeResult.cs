namespace WeddingManager.Domain.Enums;

public enum EventGuestChangeResult
{
    Added,
    Removed,
    NotFound,
    Unauthorized,
    AlreadyExists,
    NotInEvent
}
