using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext context) : base(context) { }

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
