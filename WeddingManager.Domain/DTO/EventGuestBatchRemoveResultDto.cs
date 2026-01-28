using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class EventGuestBatchRemoveResultDto
{
    public EventGuestChangeResult Status { get; set; }
    public List<Guid> RemovedGuestIds { get; set; } = [];
    public List<Guid> NotInEventGuestIds { get; set; } = [];
}
