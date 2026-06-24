namespace KidsParadiseByShoptick.AdminApp.Services;

public interface IOrderAlertsBackgroundService
{
    void Start();
    void Stop();
    bool IsRunning { get; }
}
