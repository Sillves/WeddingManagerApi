using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IEmailService
{
    Task SendRsvpConfirmationAsync(Guest guest, Wedding wedding);
    Task SendInvitationAsync(Guest guest, Wedding wedding);
    Task SendPasswordResetAsync(string email, string resetLink, string language = "en");
    Task SendPlannerInvitationAsync(WeddingInvitation invitation, Wedding wedding);
}
