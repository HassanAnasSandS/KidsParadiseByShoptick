using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface ISiteImageRepository : IRepository<SiteImage>
{
    Task<SiteImage?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteImage>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
}
