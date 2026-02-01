namespace WeddingManager.Domain.Interfaces;

public interface IObjectStorageService
{
    Task<string> UploadAsync(Stream stream, string key, string contentType);
    Task<Stream?> GetAsync(string key);
    Task DeleteAsync(string key);
}
