namespace WeddingManager.Domain.DTO;

/// <summary>
/// Public website envelope. When the wedding is gated behind passcoded invitation flows and the
/// caller has no valid flow session, <see cref="RequiresPasscode"/> is true and <see cref="Website"/>
/// is null (nothing is revealed). Otherwise the website is returned with its events filtered to the
/// unlocked flow.
/// </summary>
public class PublicWebsiteResponseDto
{
    public bool RequiresPasscode { get; set; }
    public PublicWeddingWebsiteDto? Website { get; set; }
}
