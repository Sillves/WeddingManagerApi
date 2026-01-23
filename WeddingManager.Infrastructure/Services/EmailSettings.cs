namespace WeddingManager.Infrastructure.Services;

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string FromAddress { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(Host)
               && !string.IsNullOrWhiteSpace(FromAddress);
    }
}
