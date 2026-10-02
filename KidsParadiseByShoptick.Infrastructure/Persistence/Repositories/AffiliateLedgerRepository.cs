using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Enums;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class AffiliateLedgerRepository : Repository<AffiliateLedgerEntry>, IAffiliateLedgerRepository
{
    public AffiliateLedgerRepository(AppDbContext context) : base(context) { }

    public async Task<AffiliateLedgerEntry?> GetCommissionForOrderAsync(int orderId, CancellationToken cancellationToken = default)
        => await DbSet
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(
                x => x.OrderId == orderId && x.Type == AffiliateLedgerEntryType.Commission,
                cancellationToken);

    public async Task<IReadOnlyList<AffiliateLedgerEntry>> GetByPartnerAsync(
        int partnerId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.AffiliatePartnerId == partnerId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task<decimal> SumByPartnerAndTypeAsync(
        int partnerId, AffiliateLedgerEntryType type, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.AffiliatePartnerId == partnerId && x.Type == type)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

    public async Task<decimal> SumByOrderAndTypeAsync(
        int orderId, AffiliateLedgerEntryType type, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.OrderId == orderId && x.Type == type)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
}
