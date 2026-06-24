using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsParadiseByShoptick.AdminApp.Models;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp.ViewModels;

public partial class NotificationsViewModel : ObservableObject
{
    private readonly OrderNotificationService _notifications;

    [ObservableProperty] private bool notificationsEnabled;
    [ObservableProperty] private bool isListening;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private int unreadCount;
    [ObservableProperty] private string connectionInfo = "Kids Paradise API (SignalR)";

    public ObservableCollection<OrderNotificationItem> Items { get; } = [];

    public NotificationsViewModel(OrderNotificationService notifications)
    {
        _notifications = notifications;
        _notifications.Changed += SyncFromService;
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        _notifications.LoadSettings();
        SyncFromService();
        if (_notifications.NotificationsEnabled && !_notifications.IsListening)
            _notifications.Start();
        await Task.CompletedTask;
    }

    [RelayCommand]
    void Disappearing() { }

    void SyncFromService()
    {
        NotificationsEnabled = _notifications.NotificationsEnabled;
        IsListening = _notifications.IsListening;
        StatusText = _notifications.StatusText;
        UnreadCount = _notifications.UnreadCount;
        Items.Clear();
        foreach (var item in _notifications.History)
            Items.Add(item);
    }

    partial void OnNotificationsEnabledChanged(bool value)
    {
        if (value != _notifications.NotificationsEnabled)
            _notifications.SetEnabled(value);
    }

    [RelayCommand]
    async Task RequestPermissionAsync()
    {
        var granted = await OrderNotificationService.RequestPermissionAsync();
        await Shell.Current.DisplayAlert(
            "Notifications",
            granted ? "Permission granted. You will receive order alerts." : "Permission denied. Enable notifications in phone Settings.",
            "OK");
    }

    [RelayCommand]
    async Task TestNotificationAsync()
    {
        await OrderNotificationService.RequestPermissionAsync();
        await _notifications.ShowTestNotificationAsync();
        SyncFromService();
    }

    [RelayCommand]
    async Task OpenOrdersAsync()
    {
        _notifications.MarkAllRead();
        await Shell.Current.GoToAsync("//orders");
    }

    [RelayCommand]
    async Task OpenItemAsync(OrderNotificationItem item)
    {
        _notifications.MarkRead(item);
        SyncFromService();
        await Shell.Current.GoToAsync("//orders");
    }

    [RelayCommand]
    void MarkAllRead()
    {
        _notifications.MarkAllRead();
        SyncFromService();
    }

    [RelayCommand]
    async Task ClearHistoryAsync()
    {
        if (!await Shell.Current.DisplayAlert("Clear", "Remove all notification history?", "Clear", "Cancel"))
            return;
        _notifications.ClearHistory();
        SyncFromService();
    }
}
