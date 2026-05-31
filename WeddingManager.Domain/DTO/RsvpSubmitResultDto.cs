namespace WeddingManager.Domain.DTO;

public class RsvpSubmitResultDto
{
    public Guid ResponseId { get; set; }
    public Guid GuestId { get; set; }
    public Guid? PlusOneGuestId { get; set; }
}
