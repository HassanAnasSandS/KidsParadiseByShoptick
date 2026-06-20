using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class SitemapRepository : ISitemapRepository
{
    private readonly AppDbContext _context;

    public SitemapRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<SitemapEntityEntry>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => await _context.ToyCategories
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new SitemapEntityEntry(c.Id, c.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SitemapEntityEntry>> GetAvailableProductsAsync(CancellationToken cancellationToken = default)
        => await _context.Toys
            .AsNoTracking()
            .Where(t => !t.IsSold)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SitemapEntityEntry(t.Id, t.CreatedAt))
            .ToListAsync(cancellationToken);
}
