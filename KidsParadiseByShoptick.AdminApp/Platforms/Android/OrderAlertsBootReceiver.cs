using Android.App;
using Android.Content;

namespace KidsParadiseByShoptick.AdminApp;

[BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted })]
public class OrderAlertsBootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null)
            return;

        Platforms.Android.OrderAlertsServiceStarter.TryStart(context);
    }
}
