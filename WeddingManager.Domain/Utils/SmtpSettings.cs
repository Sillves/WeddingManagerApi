namespace WeddingManager.Domain.Utils;

public class SmtpSettings
{
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string FromAddress { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool UseSsl { get; set; }
}