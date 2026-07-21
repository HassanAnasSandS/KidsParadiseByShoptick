using System.Text.Json.Serialization;
using KidsParadiseByShoptick.AdminApp.Config;
using Microsoft.AspNetCore.SignalR.Client;

namespace KidsParadiseByShoptick.AdminApp.Services;

public class SocialPostAlertService
{
    private readonly AuthSession _session;
    private HubConnection? _connection;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _sync = new(1, 1);

    public SocialPostAlertService(AuthSession session)
    {
        _session = session;
        _session.SessionChanged += () => _ = RestartAsync();
    }

    public void Start() => _ = RestartAsync();

    public async Task StopAsync()
    {
        await _sync.WaitAsync();
        try
        {
            _cts?.Cancel();
            if (_connection is not null)
                await _connection.DisposeAsync();
            _connection = null;
            _cts = null;
        }
        finally
        {
            _sync.Release();
        }
    }

    async Task RestartAsync()
    {
        await StopAsync();
        if (!_session.IsLoggedIn)
            return;

        await _sync.WaitAsync();
        try
        {
            _cts = new CancellationTokenSource();
            _ = RunAsync(_cts.Token);
        }
        finally
        {
            _sync.Release();
        }
    }

    async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var connection = BuildConnection();
                _connection = connection;
                connection.On<SocialPostAlertPayload>("SocialPostComplete", HandleAsync);

                await connection.StartAsync(cancellationToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }

    static HubConnection BuildConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl(AppSettings.OrderAlertsHubUrl, options =>
            {
                options.AccessTokenProvider = () =>
                {
                    var token = AuthSession.GetStoredToken();
                    return Task.FromResult(token)!;
                };
            })
            .WithAutomaticReconnect()
            .Build();
    }

    static async Task HandleAsync(SocialPostAlertPayload payload)
    {
        var succeeded = payload.FacebookPosted || payload.InstagramPosted
            || payload.WhatsAppCatalogPosted || payload.TikTokPosted || payload.PinterestPosted;
        var title = succeeded ? "Social post succeeded" : "Social post failed";
        var message = BuildMessage(payload);

#if ANDROID
        if (!MainThread.IsMainThread || Application.Current?.Windows.Count == 0)
        {
            Platforms.Android.AndroidOrderNotificationHelper.Show(title, message);
            return;
        }
#endif

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current is null)
                return;

            await Shell.Current.DisplayAlert(title, message, "OK");
        });
    }

    static string BuildMessage(SocialPostAlertPayload payload)
    {
        var lines = new List<string> { payload.ToyName };

        if (payload.FacebookPosted)
            lines.Add("Facebook: posted");
        else if (!string.IsNullOrWhiteSpace(payload.Message))
            lines.Add($"Facebook: not posted");

        if (payload.InstagramPosted)
            lines.Add("Instagram: posted");
        else if (!string.IsNullOrWhiteSpace(payload.Message))
            lines.Add("Instagram: not posted");

        if (payload.WhatsAppCatalogPosted)
            lines.Add("Meta catalog: updated (WhatsApp app may need WABA sync — see Social Settings Step 3)");

        if (payload.TikTokPosted)
            lines.Add("TikTok: photo post started");

        if (payload.PinterestPosted)
            lines.Add("Pinterest: pin created");

        if (!string.IsNullOrWhiteSpace(payload.Message))
            lines.Add(payload.Message!);

        return string.Join('\n', lines.Where(l => l is not null));
    }

    private sealed class SocialPostAlertPayload
    {
        [JsonPropertyName("toyId")] public int ToyId { get; set; }
        [JsonPropertyName("toyName")] public string ToyName { get; set; } = string.Empty;
        [JsonPropertyName("facebookPosted")] public bool FacebookPosted { get; set; }
        [JsonPropertyName("facebookPostId")] public string? FacebookPostId { get; set; }
        [JsonPropertyName("instagramPosted")] public bool InstagramPosted { get; set; }
        [JsonPropertyName("instagramPostId")] public string? InstagramPostId { get; set; }
        [JsonPropertyName("whatsAppCatalogPosted")] public bool WhatsAppCatalogPosted { get; set; }
        [JsonPropertyName("whatsAppCatalogProductId")] public string? WhatsAppCatalogProductId { get; set; }
        [JsonPropertyName("tikTokPosted")] public bool TikTokPosted { get; set; }
        [JsonPropertyName("tikTokPublishId")] public string? TikTokPublishId { get; set; }
        [JsonPropertyName("pinterestPosted")] public bool PinterestPosted { get; set; }
        [JsonPropertyName("pinterestPinId")] public string? PinterestPinId { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("completedAt")] public DateTimeOffset CompletedAt { get; set; }
    }
}
