using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class AffiliatePartnerRepository : Repository<AffiliatePartner>, IAffiliatePartnerRepository
{
    public AffiliatePartnerRepository(AppDbContext context) : base(context) { }

    public async Task<AffiliatePartner?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return await DbSet.FirstOrDefaultAsync(x => x.Code == normalized, cancellationToken);
    }

    public async Task<AffiliatePartner?> GetWithLedgerAsync(int id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.LedgerEntries)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AffiliatePartner>> GetAdminPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = ApplyFilters(DbSet.AsNoTracking(), search, isActive);
        return await q
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAdminAsync(string? search, bool? isActive, CancellationToken cancellationToken = default)
        => await ApplyFilters(DbSet.AsNoTracking(), search, isActive).CountAsync(cancellationToken);

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var q = DbSet.AsNoTracking().Where(x => x.Code == normalized);
        if (excludeId.HasValue)
            q = q.Where(x => x.Id != excludeId.Value);
        return await q.AnyAsync(cancellationToken);
    }

    private static IQueryable<AffiliatePartner> ApplyFilters(
        IQueryable<AffiliatePartner> q, string? search, bool? isActive)
    {
        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(x =>
                x.Name.Contains(term) ||
                x.Code.Contains(term) ||
                (x.Whatsapp != null && x.Whatsapp.Contains(term)) ||
                (x.AccountNumber != null && x.AccountNumber.Contains(term)) ||
                (x.WalletBankName != null && x.WalletBankName.Contains(term)));
        }

        return q;
    }
}
