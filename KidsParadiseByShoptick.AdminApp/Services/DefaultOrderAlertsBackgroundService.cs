namespace KidsParadiseByShoptick.AdminApp.Services;

public class DefaultOrderAlertsBackgroundService : IOrderAlertsBackgroundService
{
    private CancellationTokenSource? _cts;
    private Task? _task;

    public bool IsRunning => _task is { IsCompleted: false };

    public void Start()
    {
        if (IsRunning)
            return;

        _cts = new CancellationTokenSource();
        _task = Task.Run(() => OrderAlertListener.RunAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _task = null;
    }
}
