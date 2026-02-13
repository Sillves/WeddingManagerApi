namespace WeddingManager.Domain.DTO;

public class UpdateWeddingUserRequestDto
{
    public bool CanAccessGuests { get; set; }
    public bool CanAccessEvents { get; set; }
    public bool CanAccessExpenses { get; set; }
    public bool CanAccessWebsite { get; set; }
    public bool IsReadOnly { get; set; }
}
