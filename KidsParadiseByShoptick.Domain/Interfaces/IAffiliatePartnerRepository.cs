using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IAffiliatePartnerRepository : IRepository<AffiliatePartner>
{
    Task<AffiliatePartner?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<AffiliatePartner?> GetWithLedgerAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AffiliatePartner>> GetAdminPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAdminAsync(string? search, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}
