using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IMediaService
{
    Task<Result<MediaUploadResponseDto>> UploadAsync(Guid weddingId, Stream stream, string fileName, string contentType, long size);
    Task<Result<IEnumerable<MediaUploadResponseDto>>> GetByWeddingIdAsync(Guid weddingId);
    Task<Result<(Stream Stream, string ContentType, string FileName)>> GetStreamAsync(Guid mediaId);
    Task<Result> DeleteAsync(Guid mediaId, Guid weddingId);
}
