using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Infrastructure.Services;

public class SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    : IEmailService
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task SendRsvpConfirmationAsync(Guest guest, Wedding wedding)
    {
        if (!_settings.IsConfigured())
        {
            logger.LogInformation("Email settings not configured; skipping RSVP confirmation email.");
            return;
        }

        using var message = new MailMessage(_settings.FromAddress, guest.Email);
        
        message.Subject = $"RSVP confirmation for {wedding.Title}";
        message.Body = BuildBody(guest, wedding);

        using var client = new SmtpClient(_settings.Host, _settings.Port);
        
        client.EnableSsl = _settings.UseSsl;

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
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
