using KidsParadiseByShoptick.Domain.Interfaces;
using KidsParadiseByShoptick.Infrastructure.Persistence;
using KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;
using KidsParadiseByShoptick.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KidsParadiseByShoptick.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IToyCategoryRepository, ToyCategoryRepository>();
        services.AddScoped<IToyRepository, ToyRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IFileStorageService, FileStorageService>();

        return services;
    }
}
