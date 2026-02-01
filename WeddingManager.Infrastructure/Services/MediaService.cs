using Microsoft.AspNetCore.Http;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Infrastructure.Services;

public class MediaService(
    IMediaRepository mediaRepository,
    IObjectStorageService objectStorageService,
    IHttpContextAccessor httpContextAccessor) : IMediaService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private string GetMediaProxyUrl(Guid mediaId)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            return $"{request.Scheme}://{request.Host}/api/media/{mediaId}";
        }
        return $"/api/media/{mediaId}";
    }

    public async Task<Result<MediaUploadResponseDto>> UploadAsync(
        Guid weddingId,
        Stream stream,
        string fileName,
        string contentType,
        long size)
    {
        // Validate content type
        if (!AllowedContentTypes.Contains(contentType))
        {
            return Result<MediaUploadResponseDto>.Fail(new Error(
                ErrorCodes.Validation,
                $"File type '{contentType}' is not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}"));
        }

        // Validate file size
        if (size > MaxFileSizeBytes)
        {
            return Result<MediaUploadResponseDto>.Fail(new Error(
                ErrorCodes.Validation,
                $"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB"));
        }

        // Generate unique key
        var mediaId = Guid.NewGuid();
        var extension = Path.GetExtension(fileName);
        var key = $"weddings/{weddingId}/media/{mediaId}{extension}";

        try
        {
            // Upload to object storage (returns the key)
            var s3Key = await objectStorageService.UploadAsync(stream, key, contentType);

            // Create media record
            var media = new Media
            {
                Id = mediaId,
                FileName = fileName,
                S3Url = s3Key, // Store the S3 key, not a public URL
                ContentType = contentType,
                Size = size,
                UploadedAt = DateTime.UtcNow,
                WeddingId = weddingId
            };

            await mediaRepository.AddAsync(media);

            return Result<MediaUploadResponseDto>.Ok(new MediaUploadResponseDto
            {
                Id = media.Id,
                FileName = media.FileName,
                Url = GetMediaProxyUrl(media.Id), // Return proxy URL
                ContentType = media.ContentType,
                Size = media.Size,
                UploadedAt = media.UploadedAt
            });
        }
        catch (Exception ex)
        {
            return Result<MediaUploadResponseDto>.Fail(new Error(
                ErrorCodes.ExternalFailure,
                $"Failed to upload file: {ex.Message}"));
        }
    }

    public async Task<Result<IEnumerable<MediaUploadResponseDto>>> GetByWeddingIdAsync(Guid weddingId)
    {
        var media = await mediaRepository.GetByWeddingIdAsync(weddingId);

        var dtos = media.Select(m => new MediaUploadResponseDto
        {
            Id = m.Id,
            FileName = m.FileName,
            Url = GetMediaProxyUrl(m.Id), // Return proxy URL
            ContentType = m.ContentType,
            Size = m.Size,
            UploadedAt = m.UploadedAt
        });

        return Result<IEnumerable<MediaUploadResponseDto>>.Ok(dtos);
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> GetStreamAsync(Guid mediaId)
    {
        var media = await mediaRepository.GetByIdAsync(mediaId);
        if (media == null)
        {
            return Result<(Stream, string, string)>.Fail(new Error(ErrorCodes.NotFound, "Media not found"));
        }

        try
        {
            var stream = await objectStorageService.GetAsync(media.S3Url);
            if (stream == null)
            {
                return Result<(Stream, string, string)>.Fail(new Error(ErrorCodes.NotFound, "File not found in storage"));
            }

            return Result<(Stream, string, string)>.Ok((stream, media.ContentType, media.FileName));
        }
        catch (Exception ex)
        {
            return Result<(Stream, string, string)>.Fail(new Error(
                ErrorCodes.ExternalFailure,
                $"Failed to retrieve file: {ex.Message}"));
        }
    }

    public async Task<Result> DeleteAsync(Guid mediaId, Guid weddingId)
    {
        var media = await mediaRepository.GetByIdAsync(mediaId);
        if (media == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "Media not found"));
        }

        if (media.WeddingId != weddingId)
        {
            return Result.Fail(new Error(ErrorCodes.Forbidden, "Media does not belong to this wedding"));
        }

        try
        {
            // S3Url now stores the key directly
            await objectStorageService.DeleteAsync(media.S3Url);

            // Delete from database
            await mediaRepository.DeleteAsync(mediaId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error(
                ErrorCodes.ExternalFailure,
                $"Failed to delete file: {ex.Message}"));
        }
    }
}
