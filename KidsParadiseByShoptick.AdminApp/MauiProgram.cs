using KidsParadiseByShoptick.AdminApp.Helpers;
using KidsParadiseByShoptick.AdminApp.Services;
using KidsParadiseByShoptick.AdminApp.ViewModels;
using KidsParadiseByShoptick.AdminApp.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace KidsParadiseByShoptick.AdminApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<AuthSession>();
        builder.Services.AddSingleton<AdminApiService>();
#if ANDROID
        builder.Services.AddSingleton<IOrderAlertsBackgroundService, AndroidOrderAlertsBackgroundService>();
#else
        builder.Services.AddSingleton<IOrderAlertsBackgroundService, DefaultOrderAlertsBackgroundService>();
#endif
        builder.Services.AddSingleton<OrderNotificationService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<CategoriesViewModel>();
        builder.Services.AddTransient<CategoryEditViewModel>();
        builder.Services.AddTransient<ToysViewModel>();
        builder.Services.AddTransient<ToyEditViewModel>();
        builder.Services.AddTransient<OrdersViewModel>();
        builder.Services.AddTransient<OrderDetailViewModel>();
        builder.Services.AddTransient<OrderEditViewModel>();
        builder.Services.AddTransient<CreateOrderViewModel>();
        builder.Services.AddTransient<ReviewsViewModel>();
        builder.Services.AddTransient<ReviewEditViewModel>();
        builder.Services.AddTransient<SiteImagesViewModel>();
        builder.Services.AddTransient<NotificationsViewModel>();
        builder.Services.AddTransient<ShellViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<CategoriesPage>();
        builder.Services.AddTransient<CategoryEditPage>();
        builder.Services.AddTransient<ToysPage>();
        builder.Services.AddTransient<ToyEditPage>();
        builder.Services.AddTransient<OrdersPage>();
        builder.Services.AddTransient<OrderDetailPage>();
        builder.Services.AddTransient<OrderEditPage>();
        builder.Services.AddTransient<CreateOrderPage>();
        builder.Services.AddTransient<ReviewsPage>();
        builder.Services.AddTransient<ReviewEditPage>();
        builder.Services.AddTransient<SiteImagesPage>();
        builder.Services.AddTransient<NotificationsPage>();
        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddSingleton<ImageUrlConverter>();
        builder.Services.AddSingleton<InverseBoolConverter>();
        builder.Services.AddSingleton<NullToBoolConverter>();
        builder.Services.AddSingleton<PositiveAmountConverter>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    public static void OnAppBuilt()
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            LocalNotificationCenter.CreateNotificationChannels(
            [
                new NotificationChannelRequest
                {
                    Id = "kids_paradise_orders",
                    Name = "New Orders",
                    Description = "Kids Paradise admin order alerts",
                    Importance = AndroidImportance.High,
                },
            ]);
        }

        OrderNotificationService.RegisterTapHandler();
    }
}
