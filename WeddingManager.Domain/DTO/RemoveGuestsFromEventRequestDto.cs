namespace WeddingManager.Domain.DTO;

public class RemoveGuestsFromEventRequestDto
{
    public List<Guid> GuestIds { get; set; } = [];
}
