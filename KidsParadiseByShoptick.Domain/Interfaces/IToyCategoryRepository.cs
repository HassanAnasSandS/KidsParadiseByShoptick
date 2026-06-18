using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IToyCategoryRepository : IRepository<ToyCategory>
{
    Task<ToyCategory?> GetByIdWithToysAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToyCategory>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
}
