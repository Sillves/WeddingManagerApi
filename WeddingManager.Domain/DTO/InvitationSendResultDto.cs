namespace WeddingManager.Domain.DTO;

public class InvitationSendResultDto
{
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public List<Guid> FailedGuestIds { get; set; } = [];
}
