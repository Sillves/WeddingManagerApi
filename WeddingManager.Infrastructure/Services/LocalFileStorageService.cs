using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Infrastructure.Services;

/// <summary>
/// Local file storage service for development/testing without cloud storage.
/// Files are stored in wwwroot/uploads and served via the media proxy endpoint.
/// </summary>
public class LocalFileStorageService : IObjectStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
    {
        _basePath = Path.Combine(environment.WebRootPath ?? environment.ContentRootPath, "uploads");

        // Ensure the uploads directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> UploadAsync(Stream stream, string key, string contentType)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream);

        // Return the key (we'll proxy through our API)
        return key;
    }

    public Task<Stream?> GetAsync(string key)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(filePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string key)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
