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

    public async Task<IReadOnlyList<ToyCategory>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
}
