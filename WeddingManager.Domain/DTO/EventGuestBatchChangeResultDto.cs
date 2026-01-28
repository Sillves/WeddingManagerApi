using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class EventGuestBatchChangeResultDto
{
    public EventGuestChangeResult Status { get; set; }
    public List<Guid> AddedGuestIds { get; set; } = [];
    public List<Guid> AlreadyInEventGuestIds { get; set; } = [];
    public List<Guid> NotFoundGuestIds { get; set; } = [];
}
