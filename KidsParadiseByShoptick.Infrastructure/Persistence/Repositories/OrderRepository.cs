using KidsParadiseByShoptick.Domain.Enums;
using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context) { }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, cancellationToken);

    public async Task<Order?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .ThenInclude(i => i.Toy)
            .ThenInclude(t => t.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Order?> TrackOrderAsync(string email, string orderNumber, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .ThenInclude(i => i.Toy)
            .ThenInclude(t => t.Images)
            .FirstOrDefaultAsync(x =>
                x.OrderNumber == orderNumber &&
                x.Customer.Email == email.ToLower(), cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByCustomerEmailAsync(string email, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .ThenInclude(i => i.Toy)
            .ThenInclude(t => t.Images)
            .Where(x => x.Customer.Email == email.ToLower())
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .ThenInclude(i => i.Toy)
            .ThenInclude(t => t.Images)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasDeliveredOrderForToyAsync(int customerId, int toyId, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(o =>
            o.CustomerId == customerId &&
            o.Status == OrderStatus.Delivered &&
            o.Items.Any(i => i.ToyId == toyId), cancellationToken);

    public async Task<IReadOnlyList<Order>> GetDeliveredOrdersForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(i => i.Toy)
            .ThenInclude(t => t.Images)
            .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Delivered)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> IsToyInActiveOrderExcludingAsync(int toyId, int excludeOrderId, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(o =>
            o.Id != excludeOrderId &&
            o.Status != OrderStatus.Cancelled &&
            o.Items.Any(i => i.ToyId == toyId), cancellationToken);
}
