using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KidsParadiseByShoptick.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IToyService, ToyService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<ISiteImageService, SiteImageService>();
        services.AddScoped<ISitemapService, SitemapService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddHttpClient<YouTubeAuthService>();
        services.AddScoped<IYouTubeAuthService>(sp => sp.GetRequiredService<YouTubeAuthService>());
        services.AddSingleton<IDeliveryChargeService, DeliveryChargeService>();
        return services;
    }
}
