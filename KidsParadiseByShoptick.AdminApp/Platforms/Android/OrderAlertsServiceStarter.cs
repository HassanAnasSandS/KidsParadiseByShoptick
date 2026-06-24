using Android.Content;
using Android.OS;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp.Platforms.Android;

public static class OrderAlertsServiceStarter
{
    public static void TryStart(Context context)
    {
        if (!OrderAlertListener.ShouldRunInBackground())
            return;

        var intent = new Intent(context, typeof(OrderAlertForegroundService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public static void Stop(Context context)
    {
        var intent = new Intent(context, typeof(OrderAlertForegroundService));
        intent.SetAction(OrderAlertForegroundService.ActionStop);
        context.StartService(intent);
    }
}
