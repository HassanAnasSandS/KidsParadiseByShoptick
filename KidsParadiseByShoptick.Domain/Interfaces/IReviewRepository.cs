using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IReviewRepository : IRepository<Review>
{
    Task<IReadOnlyList<Review>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetByToyIdAsync(int toyId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForOrderAndToyAsync(int orderId, int toyId, CancellationToken cancellationToken = default);
    Task<Review?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
}
