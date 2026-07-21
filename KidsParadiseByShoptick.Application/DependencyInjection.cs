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
        services.AddSingleton<IGoogleMerchantFeedCache, GoogleMerchantFeedCache>();
        services.AddScoped<IGoogleMerchantFeedService, GoogleMerchantFeedService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddHttpClient<YouTubeAuthService>();
        services.AddScoped<IYouTubeAuthService>(sp => sp.GetRequiredService<YouTubeAuthService>());
        services.AddHttpClient<TikTokAuthService>();
        services.AddScoped<ITikTokAuthService>(sp => sp.GetRequiredService<TikTokAuthService>());
        services.AddHttpClient<ITikTokSocialService, TikTokSocialService>();
        services.AddHttpClient<PinterestAuthService>();
        services.AddScoped<IPinterestAuthService>(sp => sp.GetRequiredService<PinterestAuthService>());
        services.AddHttpClient<IPinterestSocialService, PinterestSocialService>();
        services.AddHttpClient<IMetaTokenService, MetaTokenService>();
        services.AddHttpClient<ISocialMediaService, MetaSocialMediaService>();
        services.AddHttpClient<IMetaRequirementsService, MetaRequirementsService>();
        services.AddSingleton<ISocialMediaSettingsService, SocialMediaSettingsService>();
        services.AddSingleton<IDeliveryChargeService, DeliveryChargeService>();
        return services;
    }
}
