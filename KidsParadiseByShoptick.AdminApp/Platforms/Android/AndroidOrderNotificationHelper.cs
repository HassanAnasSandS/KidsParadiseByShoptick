using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace KidsParadiseByShoptick.AdminApp.Platforms.Android;

public static class AndroidOrderNotificationHelper
{
    public static void Show(string title, string body)
    {
        var context = global::Android.App.Application.Context;
        EnsureChannel(context);

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? string.Empty);
        PendingIntent? pendingIntent = null;
        if (launchIntent is not null)
        {
            pendingIntent = PendingIntent.GetActivity(
                context,
                0,
                launchIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        }

        var builder = new NotificationCompat.Builder(context, "kids_paradise_orders")
            .SetContentTitle(title)
            .SetContentText(body)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(body))
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetPriority((int)NotificationPriority.High)
            .SetAutoCancel(true);

        if (pendingIntent is not null)
            builder.SetContentIntent(pendingIntent);

        NotificationManagerCompat.From(context).Notify(Random.Shared.Next(1000, 99999), builder.Build());
    }

    private static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        var channel = new NotificationChannel(
            "kids_paradise_orders",
            "New Orders",
            NotificationImportance.High)
        {
            Description = "Kids Paradise admin order alerts",
        };
        manager?.CreateNotificationChannel(channel);
    }
}
