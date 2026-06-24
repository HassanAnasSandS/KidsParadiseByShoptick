using KidsParadiseByShoptick.AdminApp.Models;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp.Services;

public class OrderNotificationService
{
    private readonly AuthSession _session;
    private readonly IOrderAlertsBackgroundService _background;

    public event Action? Changed;

    public OrderNotificationService(AuthSession session, IOrderAlertsBackgroundService background)
    {
        _session = session;
        _background = background;
        _session.SessionChanged += OnSessionChanged;
        LoadSettings();
    }

    public bool NotificationsEnabled { get; private set; } = true;
    public bool IsListening { get; private set; }
    public string StatusText { get; private set; } = "Stopped";
    public int UnreadCount => History.Count(n => !n.IsRead);

    public IReadOnlyList<OrderNotificationItem> History { get; private set; } = [];

    public void LoadSettings()
    {
        NotificationsEnabled = OrderAlertListener.NotificationsEnabled;
        History = OrderAlertListener.LoadHistory();
        IsListening = _background.IsRunning;
        StatusText = ResolveStatusText();
        RaiseChanged();
    }

    public void SetEnabled(bool enabled)
    {
        NotificationsEnabled = enabled;
        Preferences.Set(OrderAlertListener.EnabledKey, enabled);
        if (enabled && _session.IsLoggedIn)
            Start();
        else
            Stop();
        StatusText = ResolveStatusText();
        RaiseChanged();
    }

    public void Start()
    {
        if (!NotificationsEnabled || !_session.IsLoggedIn)
            return;

        Preferences.Set(OrderAlertListener.AlertsActiveKey, true);
        _background.Start();
        IsListening = true;
        StatusText = "Listening for new orders";
        RaiseChanged();
    }

    public void Stop()
    {
        Preferences.Set(OrderAlertListener.AlertsActiveKey, false);
        _background.Stop();
        IsListening = false;
        StatusText = NotificationsEnabled ? "Stopped" : "Alerts paused";
        RaiseChanged();
    }

    public void MarkAllRead()
    {
        foreach (var item in History)
            item.IsRead = true;
        OrderAlertListener.SaveHistory(History.ToList());
        RaiseChanged();
    }

    public void MarkRead(OrderNotificationItem item)
    {
        item.IsRead = true;
        OrderAlertListener.SaveHistory(History.ToList());
        RaiseChanged();
    }

    public void ClearHistory()
    {
        History = [];
        OrderAlertListener.SaveHistory(History);
        RaiseChanged();
    }

    public async Task ShowTestNotificationAsync()
    {
        var item = new OrderNotificationItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Test — New Order KP-TEST",
            Body = "🧸 Test notification\nOrder #: KP-TEST\nCustomer: Test Customer\nTotal: Rs. 1,500",
        };
        await OrderAlertListener.DeliverAsync(item);
        LoadSettings();
    }

    public static async Task<bool> RequestPermissionAsync()
    {
        if (DeviceInfo.Platform != DevicePlatform.Android)
            return true;

        return await Plugin.LocalNotification.LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public static void RegisterTapHandler()
    {
        Plugin.LocalNotification.LocalNotificationCenter.Current.NotificationActionTapped += async _ =>
        {
            if (Shell.Current is null) return;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.GoToAsync("//orders");
            });
        };
    }

    private void OnSessionChanged()
    {
        if (_session.IsLoggedIn && NotificationsEnabled)
            Start();
        else
            Stop();

        LoadSettings();
    }

    private string ResolveStatusText()
    {
        if (!NotificationsEnabled)
            return "Alerts paused";
        if (!_session.IsLoggedIn)
            return "Login to receive alerts";
        if (_background.IsRunning)
            return "Connected — waiting for orders";
        return "Stopped";
    }

    private void RaiseChanged() => Changed?.Invoke();
}
