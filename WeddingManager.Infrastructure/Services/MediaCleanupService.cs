using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Infrastructure.Services;

/// <summary>
/// Background service that cleans up media files 2 weeks after the wedding date.
/// Runs daily at 3 AM UTC.
/// </summary>
public class MediaCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<MediaCleanupService> logger) : BackgroundService
{
    private const int DaysAfterWeddingToCleanup = 14;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Media cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = GetNextRunTime(now);
            var delay = nextRun - now;

            logger.LogInformation("Next media cleanup scheduled for {NextRun}", nextRun);

            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await CleanupExpiredMediaAsync(stoppingToken);
            }
        }
    }

    private static DateTime GetNextRunTime(DateTime now)
    {
        // Run at 3 AM UTC daily
        var today3Am = now.Date.AddHours(3);
        return now < today3Am ? today3Am : today3Am.AddDays(1);
    }

    private async Task CleanupExpiredMediaAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting media cleanup job");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var weddingRepository = scope.ServiceProvider.GetRequiredService<IWeddingRepository>();
            var mediaRepository = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
            var objectStorageService = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();

            var cutoffDate = DateTime.UtcNow.AddDays(-DaysAfterWeddingToCleanup);
            var weddingsToCleanup = await weddingRepository.GetWeddingsWithMediaOlderThanAsync(cutoffDate);

            var totalDeleted = 0;

            foreach (var wedding in weddingsToCleanup)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                logger.LogInformation(
                    "Cleaning up media for wedding {WeddingId} (date: {WeddingDate})",
                    wedding.Id, wedding.Date);

                foreach (var media in wedding.Media.ToList())
                {
                    try
                    {
                        // Delete from object storage
                        await objectStorageService.DeleteAsync(media.S3Url);

                        // Delete from database
                        await mediaRepository.DeleteAsync(media.Id);

                        totalDeleted++;

                        logger.LogDebug("Deleted media {MediaId} for wedding {WeddingId}",
                            media.Id, wedding.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Failed to delete media {MediaId} for wedding {WeddingId}",
                            media.Id, wedding.Id);
                    }
                }
            }

            logger.LogInformation("Media cleanup completed. Deleted {Count} files", totalDeleted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Media cleanup job failed");
        }
    }
}
