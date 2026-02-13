namespace WeddingManager.Domain.DTO;

public class CreateWeddingInvitationRequestDto
{
    public string Email { get; set; } = string.Empty;
    public bool CanAccessGuests { get; set; } = true;
    public bool CanAccessEvents { get; set; } = true;
    public bool CanAccessExpenses { get; set; } = true;
    public bool CanAccessWebsite { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
}
