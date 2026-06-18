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
        services.AddSingleton<IDeliveryChargeService, DeliveryChargeService>();
        return services;
    }
}
