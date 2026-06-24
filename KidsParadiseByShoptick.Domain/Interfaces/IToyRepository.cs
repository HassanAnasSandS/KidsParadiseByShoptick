using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IToyRepository : IRepository<Toy>
{
    Task<IReadOnlyList<Toy>> GetAvailableAsync(
        int? categoryId, string? search, bool? onSale, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAvailableAsync(int? categoryId, string? search, bool? onSale, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Toy>> GetLatestAvailableAsync(int count, CancellationToken cancellationToken = default);
    Task<Toy?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<Toy?> GetWithImagesAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Toy>> GetAllAdminWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Toy>> GetAdminPagedAsync(
        int? categoryId, string? search, bool? isSold, bool? onSale, string? sort,
        int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAdminAsync(
        int? categoryId, string? search, bool? isSold, bool? onSale, CancellationToken cancellationToken = default);
}
