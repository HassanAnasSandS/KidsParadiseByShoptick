using System.Text.Json;
using KidsParadiseByShoptick.AdminApp.Config;
using KidsParadiseByShoptick.AdminApp.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace KidsParadiseByShoptick.AdminApp.Services;

public static class OrderAlertListener
{
    public const string EnabledKey = "notifications_enabled";
    public const string HistoryKey = "notification_history";
    public const string AlertsActiveKey = "order_alerts_active";
    private const int MaxHistory = 50;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static bool NotificationsEnabled => Preferences.Get(EnabledKey, true);

    public static bool ShouldRunInBackground() =>
        Preferences.Get(AlertsActiveKey, false)
        && NotificationsEnabled
        && HasActiveSession();

    public static bool HasActiveSession()
    {
        var token = AuthSession.GetStoredToken();
        return !string.IsNullOrEmpty(token) && !AuthSession.IsTokenExpired(token);
    }

    public static async Task RunAsync(CancellationToken cancellationToken, Action<string>? onStatusChanged = null)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!NotificationsEnabled || !HasActiveSession())
            {
                onStatusChanged?.Invoke(NotificationsEnabled ? "Waiting for login…" : "Alerts paused");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                continue;
            }

            HubConnection? connection = null;
            try
            {
                connection = BuildConnection(onStatusChanged);
                connection.On<OrderAlertPayload>("NewOrder", async payload =>
                {
                    var item = new OrderNotificationItem
                    {
                        Id = payload.Id,
                        Title = payload.Title,
                        Body = payload.Body,
                        ReceivedAt = payload.ReceivedAt.LocalDateTime,
                    };
                    await DeliverAsync(item);
                });

                await connection.StartAsync(cancellationToken);
                onStatusChanged?.Invoke("Connected — waiting for orders");
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                onStatusChanged?.Invoke("Reconnecting…");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
            finally
            {
                if (connection is not null)
                    await connection.DisposeAsync();
            }
        }
    }

    private static HubConnection BuildConnection(Action<string>? onStatusChanged)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(AppSettings.OrderAlertsHubUrl, options =>
            {
                options.AccessTokenProvider = () =>
                {
                    var token = AuthSession.GetStoredToken();
                    return Task.FromResult(token)!;
                };
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30),
            })
            .Build();

        connection.Reconnecting += _ =>
        {
            onStatusChanged?.Invoke("Reconnecting…");
            return Task.CompletedTask;
        };
        connection.Reconnected += _ =>
        {
            onStatusChanged?.Invoke("Connected — waiting for orders");
            return Task.CompletedTask;
        };
        connection.Closed += _ =>
        {
            onStatusChanged?.Invoke("Disconnected — retrying…");
            return Task.CompletedTask;
        };

        return connection;
    }

    public static async Task DeliverAsync(OrderNotificationItem item)
    {
        var history = LoadHistory();
        history.Insert(0, item);
        if (history.Count > MaxHistory)
            history.RemoveRange(MaxHistory, history.Count - MaxHistory);
        SaveHistory(history);

#if ANDROID
        if (!MainThread.IsMainThread || Application.Current?.Windows.Count == 0)
        {
            Platforms.Android.AndroidOrderNotificationHelper.Show(item.Title, item.Preview);
            return;
        }
#endif

        var request = new NotificationRequest
        {
            NotificationId = Random.Shared.Next(1000, 99999),
            Title = item.Title,
            Description = item.Preview,
            ReturningData = "orders",
            CategoryType = NotificationCategoryType.Status,
            Android = new AndroidOptions
            {
                ChannelId = "kids_paradise_orders",
                Priority = AndroidPriority.High,
                VibrationPattern = [200, 100, 200],
            },
        };

        if (MainThread.IsMainThread)
            await LocalNotificationCenter.Current.Show(request);
        else
            await MainThread.InvokeOnMainThreadAsync(() => LocalNotificationCenter.Current.Show(request));
    }

    public static List<OrderNotificationItem> LoadHistory()
    {
        var json = Preferences.Get(HistoryKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<OrderNotificationItem>>(json, JsonOptions)?
                .OrderByDescending(x => x.ReceivedAt)
                .Take(MaxHistory)
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void SaveHistory(IReadOnlyList<OrderNotificationItem> history)
    {
        var json = JsonSerializer.Serialize(history.Take(MaxHistory).ToList(), JsonOptions);
        Preferences.Set(HistoryKey, json);
    }

    private sealed class OrderAlertPayload
    {
        public string Id { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTimeOffset ReceivedAt { get; set; }
    }
}
