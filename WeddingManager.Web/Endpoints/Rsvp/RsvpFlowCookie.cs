using Microsoft.AspNetCore.DataProtection;

namespace WeddingManager.Web.Endpoints.Rsvp;

/// <summary>
/// Signed, short-lived cookie proving a guest unlocked (or reached an open) RSVP flow.
/// Payload binds wedding + flow + expiry; no server-side session is stored.
/// </summary>
public static class RsvpFlowCookie
{
    public const string Name = "rsvp_flow";
    private const string Purpose = "rsvp-flow-unlock.v1";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    public static void Issue(HttpContext http, IDataProtectionProvider dp, Guid weddingId, Guid flowId)
    {
        var expires = DateTimeOffset.UtcNow.Add(Lifetime);
        var payload = $"{weddingId:N}|{flowId:N}|{expires.ToUnixTimeSeconds()}";
        var token = dp.CreateProtector(Purpose).Protect(payload);

        http.Response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !http.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = Lifetime
        });
    }

    /// <summary>
    /// Returns the unlocked flow id from a valid, unexpired cookie. The flow's membership of the
    /// wedding is verified downstream by the submit service, so the wedding binding is not re-checked here.
    /// </summary>
    public static Guid? ResolveFlowId(HttpContext http, IDataProtectionProvider dp)
    {
        if (!http.Request.Cookies.TryGetValue(Name, out var token) || string.IsNullOrEmpty(token))
            return null;

        try
        {
            var payload = dp.CreateProtector(Purpose).Unprotect(token);
            var parts = payload.Split('|');
            if (parts.Length != 3) return null;
            if (!Guid.TryParseExact(parts[1], "N", out var flowId)) return null;
            if (!long.TryParse(parts[2], out var expSeconds)) return null;
            if (DateTimeOffset.FromUnixTimeSeconds(expSeconds) < DateTimeOffset.UtcNow) return null;
            return flowId;
        }
        catch
        {
            return null;
        }
    }
}
