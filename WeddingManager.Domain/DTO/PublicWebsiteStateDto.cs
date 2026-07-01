namespace WeddingManager.Domain.DTO;

/// <summary>
/// Public state for a wedding website. When the wedding has passcode-protected flows and the
/// caller has not unlocked one, <see cref="RequiresPasscode"/> is true and <see cref="Website"/>
/// is null so no content leaks to guests, crawlers, or link previews.
/// </summary>
public class PublicWebsiteStateDto
{
    public bool RequiresPasscode { get; set; }
    public PublicWeddingWebsiteDto? Website { get; set; }
}
