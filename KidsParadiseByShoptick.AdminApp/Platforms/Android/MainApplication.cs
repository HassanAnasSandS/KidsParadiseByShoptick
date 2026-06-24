using Android.App;
using Android.Runtime;
using KidsParadiseByShoptick.AdminApp.Platforms.Android;

namespace KidsParadiseByShoptick.AdminApp;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();
        OrderAlertsServiceStarter.TryStart(this);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
