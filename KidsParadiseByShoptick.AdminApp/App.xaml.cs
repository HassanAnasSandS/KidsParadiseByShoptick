using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp;

public partial class App : Application
{
    private readonly AuthSession _session;
    private readonly OrderNotificationService _notifications;
    private readonly SocialPostAlertService _socialPostAlerts;
    private readonly AppShell _shell;
    private bool _startupHandled;

    public App(AuthSession session, OrderNotificationService notifications, SocialPostAlertService socialPostAlerts, AppShell shell)
    {
        InitializeComponent();
        _session = session;
        _notifications = notifications;
        _socialPostAlerts = socialPostAlerts;
        _shell = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_shell);
        window.Activated += OnWindowActivated;
        return window;
    }

    async void OnWindowActivated(object? sender, EventArgs e)
    {
        if (_startupHandled)
            return;

        _startupHandled = true;
        await _session.LoadAsync();

        if (_session.IsLoggedIn)
        {
            if (OrderAlertListener.NotificationsEnabled)
                _notifications.Start();
            _socialPostAlerts.Start();
            await Shell.Current.GoToAsync("//dashboard");
        }
        else
        {
            _notifications.Stop();
            await _socialPostAlerts.StopAsync();
            await Shell.Current.GoToAsync("//login");
        }
    }
}
