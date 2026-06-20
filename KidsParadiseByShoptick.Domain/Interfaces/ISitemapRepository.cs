namespace KidsParadiseByShoptick.Domain.Interfaces;

public record SitemapEntityEntry(int Id, DateTime LastModified);

public interface ISitemapRepository
{
    Task<IReadOnlyList<SitemapEntityEntry>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SitemapEntityEntry>> GetAvailableProductsAsync(CancellationToken cancellationToken = default);
}
