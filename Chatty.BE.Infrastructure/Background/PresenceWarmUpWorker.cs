using Chatty.BE.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chatty.BE.Infrastructure.Background;

/// <summary>
/// Background worker that warms up the presence cache at application startup.
/// This ensures that real-time status is available immediately even after a system restart.
/// </summary>
public class PresenceWarmUpWorker(
    IServiceProvider serviceProvider,
    ILogger<PresenceWarmUpWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Presence Warm-up Worker is starting...");

        try
        {
            // The PresenceService is registered as Scoped, so we must create a scope to resolve it
            using var scope = serviceProvider.CreateScope();
            var presenceService = scope.ServiceProvider.GetRequiredService<IPresenceService>();

            var result = await presenceService.WarmUpCacheAsync(stoppingToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Presence cache warm-up completed successfully.");
            }
            else
            {
                logger.LogWarning("Presence cache warm-up failed: {Error}", result.Error);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Presence warm-up was cancelled during shutdown.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled error occurred during presence cache warm-up.");
        }

        // This worker is intended to run once at startup.
        // The task completes here, but the background service stays alive (it just won't do anything else).
        logger.LogInformation("Presence Warm-up Worker has finished its task.");
    }
}
