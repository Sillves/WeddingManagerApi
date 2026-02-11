namespace WeddingManager.Domain.Models;

public static class ErrorCodes
{
    public const string Validation = "validation";
    public const string NotFound = "not_found";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string Conflict = "conflict";
    public const string LimitExceeded = "limit_exceeded";
    public const string ExternalFailure = "external_failure";
    public const string AccountLocked = "account_locked";
    public const string Unexpected = "unexpected";
}
