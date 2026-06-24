using Android.Content;
using KidsParadiseByShoptick.AdminApp.Platforms.Android;

namespace KidsParadiseByShoptick.AdminApp.Services;

public class AndroidOrderAlertsBackgroundService : IOrderAlertsBackgroundService
{
    public bool IsRunning { get; private set; }

    public void Start()
    {
        OrderAlertsServiceStarter.TryStart(Platform.AppContext);
        IsRunning = true;
    }

    public void Stop()
    {
        OrderAlertsServiceStarter.Stop(Platform.AppContext);
        IsRunning = false;
    }
}
