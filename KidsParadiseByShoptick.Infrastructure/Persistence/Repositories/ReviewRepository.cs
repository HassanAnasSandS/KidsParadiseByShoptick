using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Review>> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplySearch(DetailsQuery(), search);
        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, CancellationToken cancellationToken = default)
        => ApplySearch(DetailsQuery(), search).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Review>> GetByToyIdPagedAsync(
        int toyId, int page, int pageSize, CancellationToken cancellationToken = default)
        => await DetailsQuery()
            .Where(x => x.ToyId == toyId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountByToyIdAsync(int toyId, CancellationToken cancellationToken = default)
        => DetailsQuery().CountAsync(x => x.ToyId == toyId, cancellationToken);

    private IQueryable<Review> DetailsQuery()
        => DbSet
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Toy)
            .Include(x => x.Order);

    private static IQueryable<Review> ApplySearch(IQueryable<Review> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = search.Trim().ToLower();
        return query.Where(x =>
            x.ReviewerName.ToLower().Contains(term)
            || x.Toy.Name.ToLower().Contains(term)
            || x.Order.OrderNumber.ToLower().Contains(term)
            || x.Comment.ToLower().Contains(term));
    }

    public async Task<IReadOnlyList<Review>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Toy)
            .Include(x => x.Order)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Review>> GetByToyIdAsync(int toyId, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Order)
            .Where(x => x.ToyId == toyId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsForOrderAndToyAsync(int orderId, int toyId, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(x => x.OrderId == orderId && x.ToyId == toyId, cancellationToken);

    public async Task<Review?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Customer)
            .Include(x => x.Toy)
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
