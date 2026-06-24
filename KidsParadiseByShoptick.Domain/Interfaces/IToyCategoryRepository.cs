using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IToyCategoryRepository : IRepository<ToyCategory>
{
    Task<ToyCategory?> GetByIdWithToysAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToyCategory>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToyCategory>> GetPublicPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountPublicAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToyCategory>> GetAdminPagedAsync(
        string? search, string? toyFilter, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAdminAsync(
        string? search, string? toyFilter, CancellationToken cancellationToken = default);
}
