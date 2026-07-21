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
        // Delay without letting cancel bubble as an unhandled fault (common on debug Stop/Restart).
        if (!await DelayQuietlyAsync(TimeSpan.FromSeconds(15), stoppingToken))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunMaintenanceAsync(stoppingToken);
            if (!await DelayQuietlyAsync(Interval, stoppingToken))
                return;
        }
    }

    static async Task<bool> DelayQuietlyAsync(TimeSpan delay, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return false;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = token.Register(static s => ((TaskCompletionSource)s!).TrySetResult(), tcs);
        var completed = await Task.WhenAny(Task.Delay(delay), tcs.Task);
        return completed != tcs.Task && !token.IsCancellationRequested;
    }

    async Task RunMaintenanceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var youTube = scope.ServiceProvider.GetRequiredService<IYouTubeAuthService>();
            var meta = scope.ServiceProvider.GetRequiredService<IMetaTokenService>();
            var tikTok = scope.ServiceProvider.GetRequiredService<ITikTokAuthService>();

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

            if (tikTok.IsConnected)
            {
                if (await tikTok.TryRefreshAsync(cancellationToken))
                    _logger.LogInformation("TikTok access token refreshed during background maintenance.");
                else
                    _logger.LogWarning("TikTok background refresh failed. Reconnect TikTok from Social Settings if posts fail.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown mid-refresh — ignore.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Social token maintenance cycle failed");
        }
    }
}
