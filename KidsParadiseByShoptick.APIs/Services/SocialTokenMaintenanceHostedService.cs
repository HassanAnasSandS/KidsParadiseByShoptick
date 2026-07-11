using KidsParadiseByShoptick.Application.Interfaces;

namespace KidsParadiseByShoptick.APIs.Services;

public class SocialTokenMaintenanceHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SocialTokenMaintenanceHostedService> _logger;

    public SocialTokenMaintenanceHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SocialTokenMaintenanceHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunMaintenanceAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    async Task RunMaintenanceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var youTube = scope.ServiceProvider.GetRequiredService<IYouTubeAuthService>();
            var meta = scope.ServiceProvider.GetRequiredService<IMetaTokenService>();

            if (youTube.IsConnected)
            {
                if (await youTube.TryRefreshAsync(cancellationToken))
                    _logger.LogInformation("YouTube access token refreshed during background maintenance.");
                else
                    _logger.LogWarning("YouTube background refresh failed. Re-authorize from the admin app if uploads fail.");
            }

            if (meta.IsConfigured)
            {
                if (await meta.TryMaintainAsync(cancellationToken))
                    _logger.LogInformation("Meta Facebook/Instagram tokens maintained.");
                else
                    _logger.LogWarning("Meta background maintenance failed. Reconnect Facebook/Instagram if posts fail.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Social token maintenance cycle failed");
        }
    }
}
