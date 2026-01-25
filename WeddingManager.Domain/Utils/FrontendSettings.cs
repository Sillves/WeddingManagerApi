namespace WeddingManager.Domain.Utils;

public class FrontendSettings
{
    public string BaseUrl { get; set; } = string.Empty;

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(BaseUrl);
    }
}
