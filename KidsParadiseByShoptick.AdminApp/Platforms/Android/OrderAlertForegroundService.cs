using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using KidsParadiseByShoptick.AdminApp.Platforms.Android;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp;

[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
public class OrderAlertForegroundService : Service
{
    public const int ServiceNotificationId = 9001;
    public const string ActionStop = "shop.shoptick.kidsparadise.admin.STOP_ALERTS";

    private CancellationTokenSource? _cts;
    private Task? _listenerTask;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ActionStop)
        {
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        if (_listenerTask is { IsCompleted: false })
            return StartCommandResult.Sticky;

        StartForeground(ServiceNotificationId, BuildServiceNotification("Listening for new orders"));
        _cts = new CancellationTokenSource();
        _listenerTask = Task.Run(async () =>
        {
            try
            {
                await OrderAlertListener.RunAsync(_cts.Token, status =>
                {
                    if (status.Contains("Connected", StringComparison.Ordinal))
                        UpdateServiceNotification("Listening for new orders");
                    else if (status.Contains("Reconnect", StringComparison.Ordinal))
                        UpdateServiceNotification("Reconnecting for order alerts…");
                    else
                        UpdateServiceNotification(status);
                });
            }
            catch (System.OperationCanceledException)
            {
                // Expected on stop.
            }
        });

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        _cts?.Cancel();
        _listenerTask = null;
        base.OnDestroy();
    }

    public override void OnTaskRemoved(Intent? rootIntent)
    {
        if (OrderAlertListener.ShouldRunInBackground())
            OrderAlertsServiceStarter.TryStart(ApplicationContext);
        base.OnTaskRemoved(rootIntent);
    }

    public override IBinder? OnBind(Intent? intent) => null;

    private Notification BuildServiceNotification(string text)
    {
        EnsureChannel();

        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty);
        PendingIntent? pendingIntent = null;
        if (launchIntent is not null)
        {
            pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                launchIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        }

        var builder = new NotificationCompat.Builder(this, "kids_paradise_alert_service")
            .SetContentTitle("Kids Paradise — Order Alerts")
            .SetContentText(text)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true);

        if (pendingIntent is not null)
            builder.SetContentIntent(pendingIntent);

        return builder.Build();
    }

    private void UpdateServiceNotification(string text)
    {
        var manager = NotificationManagerCompat.From(this);
        manager.Notify(ServiceNotificationId, BuildServiceNotification(text));
    }

    private void EnsureChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        var channel = new NotificationChannel(
            "kids_paradise_alert_service",
            "Order alert service",
            NotificationImportance.Low)
        {
            Description = "Keeps order alerts running in the background",
        };
        manager?.CreateNotificationChannel(channel);
    }
}
