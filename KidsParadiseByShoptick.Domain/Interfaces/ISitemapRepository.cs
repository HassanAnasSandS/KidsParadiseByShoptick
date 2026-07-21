namespace KidsParadiseByShoptick.Domain.Interfaces;

public record SitemapEntityEntry(int Id, DateTime LastModified);

public record SitemapProductEntry(
    int Id,
    DateTime LastModified,
    string Name,
    string? PrimaryImagePath);

public interface ISitemapRepository
{
    Task<IReadOnlyList<SitemapEntityEntry>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SitemapEntityEntry>> GetAvailableProductsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SitemapProductEntry>> GetAvailableProductsWithImagesAsync(CancellationToken cancellationToken = default);
}
