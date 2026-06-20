using KidsParadiseByShoptick.Application;
using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public async Task<Customer?> GetByWhatsappAsync(string whatsapp, CancellationToken cancellationToken = default)
    {
        var key = ContactNormalizer.NormalizeWhatsapp(whatsapp);
        var customers = await DbSet.ToListAsync(cancellationToken);
        return customers.FirstOrDefault(c => ContactNormalizer.NormalizeWhatsapp(c.Whatsapp) == key);
    }
}
