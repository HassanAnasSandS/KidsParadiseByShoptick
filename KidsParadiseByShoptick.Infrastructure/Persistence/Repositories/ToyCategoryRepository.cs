using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class ToyCategoryRepository : Repository<ToyCategory>, IToyCategoryRepository
{
    public ToyCategoryRepository(AppDbContext context) : base(context) { }

    public async Task<ToyCategory?> GetByIdWithToysAsync(int id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Toys.Where(t => !t.IsSold))
            .ThenInclude(t => t.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ToyCategory>> GetAdminPagedAsync(
        string? search, string? toyFilter, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = BuildAdminQuery(search, toyFilter, sort);
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminAsync(
        string? search, string? toyFilter, CancellationToken cancellationToken = default)
        => BuildAdminQuery(search, toyFilter, sort: null).CountAsync(cancellationToken);

    private IQueryable<ToyCategory> BuildAdminQuery(string? search, string? toyFilter, string? sort)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }

        if (string.Equals(toyFilter, "Empty", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => !x.Toys.Any());
        else if (string.Equals(toyFilter, "HasToys", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Toys.Any());

        query = sort switch
        {
            "name-desc" => query.OrderByDescending(x => x.Name),
            "toys-high" => query.OrderByDescending(x => x.Toys.Count),
            "toys-low" => query.OrderBy(x => x.Toys.Count),
            _ => query.OrderBy(x => x.Name),
        };

        return query;
    }

    public async Task<IReadOnlyList<ToyCategory>> GetPublicPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountPublicAsync(CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().CountAsync(cancellationToken);

    public async Task<IReadOnlyList<ToyCategory>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
}
