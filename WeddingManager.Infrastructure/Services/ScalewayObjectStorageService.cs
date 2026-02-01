using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Infrastructure.Services;

public class ScalewayObjectStorageService : IObjectStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public ScalewayObjectStorageService(IOptions<ScalewayStorageSettings> options)
    {
        var settings = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = settings.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = settings.Region // Required for Scaleway S3 signature
        };

        _s3Client = new AmazonS3Client(settings.AccessKey, settings.SecretKey, config);
        _bucketName = settings.BucketName;
    }

    public async Task<string> UploadAsync(Stream stream, string key, string contentType)
    {
        // Copy stream to memory to calculate MD5 and allow re-reading
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var data = memoryStream.ToArray();

        // Calculate MD5 hash of the data
        var md5Hash = MD5.HashData(data);
        var expectedETag = Convert.ToHexString(md5Hash).ToLowerInvariant();

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = new MemoryStream(data),
            ContentType = contentType,
            
        };

        var response = await _s3Client.PutObjectAsync(request);

        // Validate upload by comparing ETag with calculated MD5
        // ETag is returned with quotes, so we need to trim them
        var returnedETag = response.ETag.Trim('"').ToLowerInvariant();

        if (returnedETag != expectedETag)
        {
            // Upload corrupted - delete the object and throw
            await DeleteAsync(key);
            throw new InvalidOperationException(
                $"Upload validation failed: data may be corrupted. Expected ETag: {expectedETag}, Received: {returnedETag}");
        }

        return key;
    }

    public async Task<Stream?> GetAsync(string key)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(request);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        await _s3Client.DeleteObjectAsync(request);
    }
}
