namespace WeddingManager.Domain.DTO;

public class AddGuestsToEventRequestDto
{
    public List<Guid> GuestIds { get; set; } = [];
}
