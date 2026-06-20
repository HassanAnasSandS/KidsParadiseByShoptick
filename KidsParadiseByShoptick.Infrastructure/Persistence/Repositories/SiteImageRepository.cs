using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class SiteImageRepository : Repository<SiteImage>, ISiteImageRepository
{
    public SiteImageRepository(AppDbContext context) : base(context) { }

    public async Task<SiteImage?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

    public async Task<IReadOnlyList<SiteImage>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Key).ToListAsync(cancellationToken);
}
