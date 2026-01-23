using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Infrastructure.Services;

public class SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    : IEmailService
{
    private readonly EmailSettings settings = settings.Value;

    public async Task SendRsvpConfirmationAsync(Guest guest, Wedding wedding)
    {
        if (!settings.IsConfigured())
        {
            logger.LogInformation("Email settings not configured; skipping RSVP confirmation email.");
            return;
        }

        using var message = new MailMessage(settings.FromAddress, guest.Email)
        {
            Subject = $"RSVP confirmation for {wedding.Title}",
            Body = BuildBody(guest, wedding)
        };

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        }

        await client.SendMailAsync(message);
    }

    private static string BuildBody(Guest guest, Wedding wedding)
    {
        return $"Hi {guest.Name},\n\n" +
               $"Thanks for your RSVP ({guest.RsvpStatus}) for {wedding.Title}.\n\n" +
               "We look forward to celebrating with you!\n";
    }
}
