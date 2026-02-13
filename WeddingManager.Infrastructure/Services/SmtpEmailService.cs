using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Infrastructure.Services;

public class SmtpEmailService(
    IOptions<SmtpSettings> smtpSettings,
    IOptions<FrontendSettings> frontendSettings,
    ILogger<SmtpEmailService> logger)
    : IEmailService
{
    private readonly SmtpSettings _smtpSettings = smtpSettings.Value;
    private readonly FrontendSettings _frontendSettings = frontendSettings.Value;

    public async Task SendRsvpConfirmationAsync(Guest guest, Wedding wedding)
    {
        if (!_smtpSettings.IsConfigured())
        {
            logger.LogInformation("Email settings not configured; skipping RSVP confirmation email.");
            return;
        }

        using var message = new MailMessage(_smtpSettings.FromAddress, guest.Email);
        
        message.Subject = $"RSVP confirmation for {wedding.Title}";
        message.Body = BuildBody(guest, wedding);

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
        
        client.EnableSsl = _smtpSettings.UseSsl;

        if (!string.IsNullOrWhiteSpace(_smtpSettings.Username))
        {
            client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
        }

        await client.SendMailAsync(message);
    }

    public async Task SendInvitationAsync(Guest guest, Wedding wedding)
    {
        if (!_smtpSettings.IsConfigured())
        {
            throw new InvalidOperationException("Email settings are not configured");
        }

        if (!_frontendSettings.IsConfigured())
        {
            throw new InvalidOperationException("Frontend URL is not configured");
        }

        if (string.IsNullOrWhiteSpace(guest.InvitationToken))
        {
            throw new InvalidOperationException("Guest invitation token is missing");
        }

        using var message = new MailMessage(_smtpSettings.FromAddress, guest.Email);
        var language = NormalizeLanguage(guest.PreferredLanguage);
        message.Subject = GetInvitationSubject(language, wedding.Title);
        message.Body = BuildInvitationBody(guest, wedding, BuildRsvpLink(wedding, guest.InvitationToken), language);
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
        client.EnableSsl = _smtpSettings.UseSsl;

        if (!string.IsNullOrWhiteSpace(_smtpSettings.Username))
        {
            client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
        }

        await client.SendMailAsync(message);
    }

    private static string BuildBody(Guest guest, Wedding wedding)
    {
        return $"Hi {guest.Name},\n\n" +
               $"Thanks for your RSVP ({guest.RsvpStatus}) for {wedding.Title}.\n\n" +
               "We look forward to celebrating with you!\n";
    }

    private string BuildInvitationBody(Guest guest, Wedding wedding, string rsvpLink, string language)
    {
        var culture = GetCulture(language);
        var dateText = wedding.Date.ToString("D", culture);

        var greeting = GetInvitationGreeting(language, guest.Name);
        var detailsLabel = GetInvitationDetailsLabel(language);
        var rsvpLabel = GetInvitationRsvpLabel(language);
        var footer = GetInvitationFooter(language);

        var builder = new StringBuilder();
        builder.Append("<html><body style=\"font-family: Arial, sans-serif; color: #111;\">");
        builder.Append($"<p>{WebUtility.HtmlEncode(greeting)}</p>");
        builder.Append("<p>");
        builder.Append($"{WebUtility.HtmlEncode(detailsLabel)}<br/>");
        builder.Append($"<strong>{WebUtility.HtmlEncode(wedding.Title)}</strong><br/>");
        builder.Append($"{WebUtility.HtmlEncode(dateText)}<br/>");
        builder.Append($"{WebUtility.HtmlEncode(wedding.Location)}");
        builder.Append("</p>");
        builder.Append("<p>");
        builder.Append($"<a href=\"{WebUtility.HtmlEncode(rsvpLink)}\" style=\"display:inline-block;padding:10px 16px;background:#1d4ed8;color:#fff;text-decoration:none;border-radius:6px;\">");
        builder.Append($"{WebUtility.HtmlEncode(rsvpLabel)}</a>");
        builder.Append("</p>");
        builder.Append("<p style=\"font-size:12px;color:#666;\">");
        builder.Append($"{WebUtility.HtmlEncode(footer)}<br/>");
        builder.Append($"<a href=\"{WebUtility.HtmlEncode(rsvpLink)}\" style=\"color:#1d4ed8;\">");
        builder.Append($"{WebUtility.HtmlEncode(rsvpLink)}</a>");
        builder.Append("</p>");
        builder.Append("</body></html>");

        return builder.ToString();
    }

    private string BuildRsvpLink(Wedding wedding, string token)
    {
        var baseUrl = _frontendSettings.BaseUrl.TrimEnd('/');
        var slugOrId = string.IsNullOrWhiteSpace(wedding.Slug) ? wedding.Id.ToString() : wedding.Slug;
        return $"{baseUrl}/rsvp/{slugOrId}?guest={token}";
    }

    private static string GetInvitationSubject(string language, string weddingTitle)
    {
        return language.ToLowerInvariant() switch
        {
            "nl" => $"Je bent uitgenodigd voor {weddingTitle}",
            "fr" => $"Vous êtes invité(e) à {weddingTitle}",
            _ => $"You're invited to {weddingTitle}"
        };
    }

    private static string GetInvitationGreeting(string language, string guestName)
    {
        return language.ToLowerInvariant() switch
        {
            "nl" => $"Hallo {guestName},",
            "fr" => $"Bonjour {guestName},",
            _ => $"Hi {guestName},"
        };
    }

    private static string GetInvitationDetailsLabel(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "nl" => "Details van het huwelijk:",
            "fr" => "Détails du mariage :",
            _ => "Wedding details:"
        };
    }

    private static string GetInvitationRsvpLabel(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "nl" => "Bevestig je aanwezigheid",
            "fr" => "Confirmer votre présence",
            _ => "RSVP now"
        };
    }

    private static string GetInvitationFooter(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "nl" => "Als de knop niet werkt, kopieer en plak de link in je browser.",
            "fr" => "Si le bouton ne fonctionne pas, copiez-collez le lien dans votre navigateur.",
            _ => "If the button doesn't work, copy and paste the link into your browser."
        };
    }

    private static CultureInfo GetCulture(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "nl" => new CultureInfo("nl-BE"),
            "fr" => new CultureInfo("fr-FR"),
            _ => new CultureInfo("en-US")
        };
    }

    public async Task SendPasswordResetAsync(string email, string resetLink, string language = "en")
    {
        if (!_smtpSettings.IsConfigured())
        {
            throw new InvalidOperationException("Email settings are not configured");
        }

        var normalizedLanguage = NormalizeLanguage(language);
        using var message = new MailMessage(_smtpSettings.FromAddress, email);
        message.Subject = GetPasswordResetSubject(normalizedLanguage);
        message.Body = BuildPasswordResetBody(resetLink, normalizedLanguage);
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
        client.EnableSsl = _smtpSettings.UseSsl;

        if (!string.IsNullOrWhiteSpace(_smtpSettings.Username))
        {
            client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
        }

        await client.SendMailAsync(message);
    }

    private static string GetPasswordResetSubject(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "nl" => "Wachtwoord resetten - Amare Wedding",
            "fr" => "Réinitialisation du mot de passe - Amare Wedding",
            _ => "Reset your password - Amare Wedding"
        };
    }

    private static string BuildPasswordResetBody(string resetLink, string language)
    {
        var (greeting, instructions, buttonText, footer, expiry) = language.ToLowerInvariant() switch
        {
            "nl" => (
                "Hallo,",
                "Je hebt een aanvraag ingediend om je wachtwoord te resetten. Klik op de knop hieronder om een nieuw wachtwoord in te stellen.",
                "Wachtwoord resetten",
                "Als je deze aanvraag niet hebt gedaan, kun je deze e-mail veilig negeren.",
                "Deze link is 24 uur geldig."
            ),
            "fr" => (
                "Bonjour,",
                "Vous avez demandé la réinitialisation de votre mot de passe. Cliquez sur le bouton ci-dessous pour définir un nouveau mot de passe.",
                "Réinitialiser le mot de passe",
                "Si vous n'avez pas fait cette demande, vous pouvez ignorer cet e-mail en toute sécurité.",
                "Ce lien est valable 24 heures."
            ),
            _ => (
                "Hello,",
                "You requested to reset your password. Click the button below to set a new password.",
                "Reset Password",
                "If you didn't request this, you can safely ignore this email.",
                "This link is valid for 24 hours."
            )
        };

        var builder = new StringBuilder();
        builder.Append("<html><body style=\"font-family: Arial, sans-serif; color: #111; max-width: 600px; margin: 0 auto;\">");
        builder.Append($"<p>{WebUtility.HtmlEncode(greeting)}</p>");
        builder.Append($"<p>{WebUtility.HtmlEncode(instructions)}</p>");
        builder.Append("<p style=\"margin: 24px 0;\">");
        builder.Append($"<a href=\"{WebUtility.HtmlEncode(resetLink)}\" style=\"display:inline-block;padding:12px 24px;background:#1d4ed8;color:#fff;text-decoration:none;border-radius:6px;font-weight:bold;\">");
        builder.Append($"{WebUtility.HtmlEncode(buttonText)}</a>");
        builder.Append("</p>");
        builder.Append($"<p style=\"font-size:14px;color:#666;\">{WebUtility.HtmlEncode(expiry)}</p>");
        builder.Append($"<p style=\"font-size:12px;color:#999;\">{WebUtility.HtmlEncode(footer)}</p>");
        builder.Append("<p style=\"font-size:12px;color:#666; margin-top: 24px;\">");
        builder.Append($"<a href=\"{WebUtility.HtmlEncode(resetLink)}\" style=\"color:#1d4ed8;word-break:break-all;\">");
        builder.Append($"{WebUtility.HtmlEncode(resetLink)}</a>");
        builder.Append("</p>");
        builder.Append("</body></html>");

        return builder.ToString();
    }

    public async Task SendPlannerInvitationAsync(WeddingInvitation invitation, Wedding wedding)
    {
        if (!_smtpSettings.IsConfigured())
        {
            logger.LogInformation("Email settings not configured; skipping planner invitation email.");
            return;
        }

        var frontendBaseUrl = _frontendSettings.BaseUrl;
        var inviteLink = $"{frontendBaseUrl}/invite/{invitation.Token}";
        var weddingTitle = WebUtility.HtmlEncode(wedding.Title);
        var coupleName = WebUtility.HtmlEncode($"{wedding.User?.FirstName} & {wedding.User?.LastName}");

        var subject = $"You're invited to collaborate on {wedding.Title} — Amare.Wedding";
        var body = new StringBuilder();
        body.Append("<html><body style=\"font-family: Arial, sans-serif; color: #111; max-width: 600px; margin: 0 auto;\">");
        body.Append("<h2>You've been invited!</h2>");
        body.Append($"<p>{coupleName} has invited you to collaborate on their wedding <strong>{weddingTitle}</strong> as a planner on Amare.Wedding.</p>");
        body.Append("<p style=\"margin: 30px 0;\">");
        body.Append($"<a href=\"{WebUtility.HtmlEncode(inviteLink)}\" style=\"display:inline-block;padding:12px 24px;background:#7c3aed;color:#fff;text-decoration:none;border-radius:6px;font-weight:bold;\">Accept Invitation</a>");
        body.Append("</p>");
        body.Append("<p style=\"color: #666; font-size: 14px;\">This invitation expires in 30 days.</p>");
        body.Append($"<p style=\"color: #999; font-size: 12px;\">If the button doesn't work, copy this link: {WebUtility.HtmlEncode(inviteLink)}</p>");
        body.Append("</body></html>");

        using var message = new MailMessage(_smtpSettings.FromAddress, invitation.Email);
        message.Subject = subject;
        message.Body = body.ToString();
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
        client.EnableSsl = _smtpSettings.UseSsl;

        if (!string.IsNullOrWhiteSpace(_smtpSettings.Username))
        {
            client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
        }

        await client.SendMailAsync(message);
    }

    private static string NormalizeLanguage(string? language)
    {
        return string.IsNullOrWhiteSpace(language) ? "en" : language.Trim().ToLowerInvariant();
    }
}
