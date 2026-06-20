using KidsParadiseByShoptick.Application;
using KidsParadiseByShoptick.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KidsParadiseByShoptick.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        foreach (var customer in await context.Customers.ToListAsync())
        {
            if (!string.IsNullOrWhiteSpace(customer.Whatsapp))
                continue;

            var fallback = !string.IsNullOrWhiteSpace(customer.Phone)
                ? customer.Phone
                : customer.Email;
            customer.Whatsapp = ContactNormalizer.NormalizeWhatsapp(fallback);
        }
        await context.SaveChangesAsync();

        foreach (var def in SiteImageDefaults.All)
        {
            if (await context.SiteImages.AnyAsync(x => x.Key == def.Key))
                continue;

            context.SiteImages.Add(new SiteImage
            {
                Key = def.Key,
                Label = def.Label,
                Group = def.Group,
                SortOrder = def.SortOrder,
            });
        }

        await context.SaveChangesAsync();
    }
}
