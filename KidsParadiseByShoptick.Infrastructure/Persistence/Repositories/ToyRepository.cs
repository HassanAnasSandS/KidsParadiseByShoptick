using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class ToyRepository : Repository<Toy>, IToyRepository
{
    public ToyRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Toy>> GetAvailableAsync(
        int? categoryId, string? search, bool? onSale, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplySort(BuildAvailableQuery(categoryId, search, onSale), sort);
        return await query
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAvailableAsync(int? categoryId, string? search, bool? onSale, CancellationToken cancellationToken = default)
        => await BuildAvailableQuery(categoryId, search, onSale).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Toy>> GetLatestAvailableAsync(int count, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Where(x => !x.IsSold)
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<Toy?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Include(x => x.Reviews)
            .ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Toy?> GetWithImagesAsync(int id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Toy>> GetAllAdminWithDetailsAsync(CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Toy>> GetAdminPagedAsync(
        int? categoryId, string? search, bool? isSold, bool? onSale, string? sort,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplyAdminSort(BuildAdminQuery(categoryId, search, isSold, onSale), sort);
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminAsync(
        int? categoryId, string? search, bool? isSold, bool? onSale, CancellationToken cancellationToken = default)
        => BuildAdminQuery(categoryId, search, isSold, onSale).CountAsync(cancellationToken);

    private IQueryable<Toy> BuildAdminQuery(int? categoryId, string? search, bool? isSold, bool? onSale)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }

        if (isSold == true)
            query = query.Where(x => x.IsSold);
        else if (isSold == false)
            query = query.Where(x => !x.IsSold);

        if (onSale == true)
            query = query.Where(x => x.SalePrice != null && x.SalePrice < x.Price);
        else if (onSale == false)
            query = query.Where(x => x.SalePrice == null || x.SalePrice >= x.Price);

        return query;
    }

    private static IQueryable<Toy> ApplyAdminSort(IQueryable<Toy> query, string? sort) => sort switch
    {
        "name" => query.OrderBy(x => x.Name),
        "price-low" => query.OrderBy(x => x.SalePrice ?? x.Price),
        "price-high" => query.OrderByDescending(x => x.SalePrice ?? x.Price),
        _ => query.OrderByDescending(x => x.CreatedAt),
    };

    private IQueryable<Toy> BuildAvailableQuery(int? categoryId, string? search, bool? onSale)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsSold);

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }

        if (onSale == true)
            query = query.Where(x => x.SalePrice != null && x.SalePrice < x.Price);
        else if (onSale == false)
            query = query.Where(x => x.SalePrice == null || x.SalePrice >= x.Price);

        return query;
    }

    private static IQueryable<Toy> ApplySort(IQueryable<Toy> query, string? sort) => sort switch
    {
        "name" => query.OrderBy(x => x.Name),
        "price-low" => query.OrderBy(x => x.SalePrice ?? x.Price),
        "price-high" => query.OrderByDescending(x => x.SalePrice ?? x.Price),
        _ => query.OrderByDescending(x => x.CreatedAt),
    };
}
