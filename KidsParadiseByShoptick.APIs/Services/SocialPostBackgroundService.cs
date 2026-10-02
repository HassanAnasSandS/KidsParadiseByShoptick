using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;

namespace KidsParadiseByShoptick.APIs.Services;

public class SocialPostBackgroundService : BackgroundService
{
    private readonly SocialPostQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SocialPostBackgroundService> _logger;

    public SocialPostBackgroundService(
        SocialPostQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<SocialPostBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessJobAsync(job, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal on app shutdown / debug restart.
        }
    }

    async Task ProcessJobAsync(SocialPostJob job, CancellationToken cancellationToken)
    {
        SocialPostResultDto result;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var socialMedia = scope.ServiceProvider.GetRequiredService<ISocialMediaService>();
            result = await socialMedia.PostToyAsync(job.ToyId, job.Trigger, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background social post failed for toy {ToyId} ({ToyName})", job.ToyId, job.ToyName);
            result = new SocialPostResultDto(false, null, false, null, ex.Message);
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notifier = scope.ServiceProvider.GetRequiredService<ISocialPostNotificationService>();
            await notifier.NotifyAsync(job.ToyId, job.ToyName, result, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify admin about social post result for toy {ToyId}", job.ToyId);
        }
    }
}
