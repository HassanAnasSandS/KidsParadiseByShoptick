using KidsParadiseByShoptick.Application;
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

    public async Task<IReadOnlyList<Order>> GetByCustomerWhatsappAsync(string whatsapp, CancellationToken cancellationToken = default)
    {
        var (items, _) = await GetByCustomerWhatsappPagedInternalAsync(whatsapp, 1, int.MaxValue, cancellationToken);
        return items;
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerWhatsappPagedAsync(
        string whatsapp, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, _) = await GetByCustomerWhatsappPagedInternalAsync(whatsapp, page, pageSize, cancellationToken);
        return items;
    }

    public Task<int> CountByCustomerWhatsappAsync(string whatsapp, CancellationToken cancellationToken = default)
        => GetCustomerOrdersQuery(whatsapp).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAdminPagedAsync(
        string? status, string? search, string? city, DateTime? dateFrom, DateTime? dateTo, string? sort,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplyAdminFilters(AdminDetailsQuery(), status, search, city, dateFrom, dateTo);
        query = ApplyAdminSort(query, sort);
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminAsync(
        string? status, string? search, string? city, DateTime? dateFrom, DateTime? dateTo,
        CancellationToken cancellationToken = default)
        => ApplyAdminFilters(AdminDetailsQuery(), status, search, city, dateFrom, dateTo).CountAsync(cancellationToken);

    public async Task<(int Total, int Pending, int Confirmed, int Shipped, int Delivered, int Cancelled)> GetStatusCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var grouped = await DbSet
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int Count(OrderStatus status) =>
            grouped.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        var pending = Count(OrderStatus.Pending);
        var confirmed = Count(OrderStatus.Confirmed);
        var shipped = Count(OrderStatus.Shipped);
        var delivered = Count(OrderStatus.Delivered);
        var cancelled = Count(OrderStatus.Cancelled);

        return (pending + confirmed + shipped + delivered + cancelled, pending, confirmed, shipped, delivered, cancelled);
    }

    public async Task<IReadOnlyList<string>> GetDistinctCitiesAsync(CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Select(o => o.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

    private async Task<(IReadOnlyList<Order> Items, int Total)> GetByCustomerWhatsappPagedInternalAsync(
        string whatsapp, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = GetCustomerOrdersQuery(whatsapp);
        var total = await query.CountAsync(cancellationToken);
        if (total == 0)
            return ([], 0);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    private IQueryable<Order> GetCustomerOrdersQuery(string whatsapp)
    {
        var key = ContactNormalizer.NormalizeWhatsapp(whatsapp);
        return AdminDetailsQuery()
            .Where(x => x.Customer.Whatsapp == key);
    }

    private IQueryable<Order> AdminDetailsQuery()
        => DbSet
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .ThenInclude(i => i.Toy)
            .ThenInclude(t => t.Images);

    private static IQueryable<Order> ApplyAdminFilters(
        IQueryable<Order> query, string? status, string? search, string? city,
        DateTime? dateFrom, DateTime? dateTo)
    {
        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            query = query.Where(x => x.Status == orderStatus);

        if (!string.IsNullOrWhiteSpace(city) && !string.Equals(city, "All", StringComparison.OrdinalIgnoreCase))
        {
            var cityTerm = city.Trim();
            query = query.Where(x => x.City == cityTerm);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.OrderNumber.ToLower().Contains(term)
                || x.Customer.Name.ToLower().Contains(term)
                || x.Customer.Whatsapp.Contains(term)
                || (x.TrackingNumber != null && x.TrackingNumber.ToLower().Contains(term))
                || x.City.ToLower().Contains(term));
        }

        if (dateFrom.HasValue)
            query = query.Where(x => x.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.CreatedAt <= dateTo.Value);

        return query;
    }

    private static IQueryable<Order> ApplyAdminSort(IQueryable<Order> query, string? sort)
        => string.Equals(sort, "oldest", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.CreatedAt)
            : query.OrderByDescending(x => x.CreatedAt);

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
