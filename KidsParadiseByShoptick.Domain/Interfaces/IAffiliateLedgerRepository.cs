using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Enums;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IAffiliateLedgerRepository : IRepository<AffiliateLedgerEntry>
{
    Task<AffiliateLedgerEntry?> GetCommissionForOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AffiliateLedgerEntry>> GetByPartnerAsync(int partnerId, CancellationToken cancellationToken = default);
    Task<decimal> SumByPartnerAndTypeAsync(
        int partnerId, AffiliateLedgerEntryType type, CancellationToken cancellationToken = default);
    Task<decimal> SumByOrderAndTypeAsync(
        int orderId, AffiliateLedgerEntryType type, CancellationToken cancellationToken = default);
}
