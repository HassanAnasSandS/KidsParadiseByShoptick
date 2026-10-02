using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface IAffiliateService
{
    Task<PagedResult<AffiliatePartnerDto>> GetAdminPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AffiliatePartnerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AffiliatePartnerDto> CreateAsync(CreateAffiliatePartnerRequest request, CancellationToken cancellationToken = default);
    Task<AffiliatePartnerDto?> UpdateAsync(int id, UpdateAffiliatePartnerRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<AffiliateLedgerDto?> GetLedgerAsync(int partnerId, CancellationToken cancellationToken = default);
    Task<AffiliateLedgerDto?> GetLedgerByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<AffiliateLedgerEntryDto> RecordPaymentAsync(
        int partnerId, RecordAffiliatePaymentRequest request, CancellationToken cancellationToken = default);
    Task ApplyCommissionForDeliveredOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task ReverseCommissionForOrderAsync(int orderId, CancellationToken cancellationToken = default);
}
