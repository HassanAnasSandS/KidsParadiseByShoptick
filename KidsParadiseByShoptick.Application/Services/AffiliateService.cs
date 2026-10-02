using System.Security.Cryptography;
using System.Text;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Enums;
using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

public class AffiliateService : IAffiliateService
{
    public const decimal CommissionRate = 0.10m;

    /// <summary>
    /// Commission base = product amount after discount, never including delivery.
    /// </summary>
    public static decimal GetCommissionBase(decimal subTotal, decimal? discountAmount)
    {
        var discount = discountAmount ?? 0m;
        if (discount < 0) discount = 0;
        var basis = subTotal - discount;
        return basis > 0 ? basis : 0m;
    }

    public static decimal CalculateCommission(decimal subTotal, decimal? discountAmount) =>
        decimal.Round(GetCommissionBase(subTotal, discountAmount) * CommissionRate, 2);

    private readonly IUnitOfWork _unitOfWork;

    public AffiliateService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<PagedResult<AffiliatePartnerDto>> GetAdminPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var partners = await _unitOfWork.AffiliatePartners.GetAdminPagedAsync(search, isActive, page, pageSize, cancellationToken);
        var total = await _unitOfWork.AffiliatePartners.CountAdminAsync(search, isActive, cancellationToken);
        var items = new List<AffiliatePartnerDto>();
        foreach (var partner in partners)
        {
            await MigrateLegacyCodeIfNeededAsync(partner, cancellationToken);
            items.Add(await MapPartnerAsync(partner, cancellationToken));
        }
        return new PagedResult<AffiliatePartnerDto>(items, total, page, pageSize);
    }

    public async Task<AffiliatePartnerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var partner = await _unitOfWork.AffiliatePartners.GetByIdAsync(id, cancellationToken);
        if (partner is null) return null;
        await MigrateLegacyCodeIfNeededAsync(partner, cancellationToken);
        return await MapPartnerAsync(partner, cancellationToken);
    }

    public async Task<AffiliatePartnerDto> CreateAsync(
        CreateAffiliatePartnerRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Partner name is required.");
        if (string.IsNullOrWhiteSpace(request.Whatsapp))
            throw new InvalidOperationException("WhatsApp is required.");
        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            throw new InvalidOperationException("Account number is required.");
        if (string.IsNullOrWhiteSpace(request.WalletBankName))
            throw new InvalidOperationException("Wallet / Bank name is required.");

        var partner = new AffiliatePartner
        {
            Name = name,
            Whatsapp = NormalizeOptional(request.Whatsapp),
            AccountNumber = NormalizeOptional(request.AccountNumber),
            WalletBankName = NormalizeOptional(request.WalletBankName),
            // Temporary unique placeholder until Id is assigned, then replaced with opaque hash.
            Code = $"TMP{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            IsActive = request.IsActive
        };

        await _unitOfWork.AffiliatePartners.AddAsync(partner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        partner.Code = await EnsureUniqueOpaqueCodeAsync(partner.Id, cancellationToken);
        await _unitOfWork.AffiliatePartners.UpdateAsync(partner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapPartnerAsync(partner, cancellationToken);
    }

    public async Task<AffiliatePartnerDto?> UpdateAsync(
        int id, UpdateAffiliatePartnerRequest request, CancellationToken cancellationToken = default)
    {
        var partner = await _unitOfWork.AffiliatePartners.GetByIdAsync(id, cancellationToken);
        if (partner is null) return null;

        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Partner name is required.");
        if (string.IsNullOrWhiteSpace(request.Whatsapp))
            throw new InvalidOperationException("WhatsApp is required.");
        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            throw new InvalidOperationException("Account number is required.");
        if (string.IsNullOrWhiteSpace(request.WalletBankName))
            throw new InvalidOperationException("Wallet / Bank name is required.");

        partner.Name = name;
        partner.Whatsapp = NormalizeOptional(request.Whatsapp);
        partner.AccountNumber = NormalizeOptional(request.AccountNumber);
        partner.WalletBankName = NormalizeOptional(request.WalletBankName);
        // Code / affiliate link are immutable after create — never regenerate on update.
        partner.IsActive = request.IsActive;

        await _unitOfWork.AffiliatePartners.UpdateAsync(partner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapPartnerAsync(partner, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var partner = await _unitOfWork.AffiliatePartners.GetByIdAsync(id, cancellationToken);
        if (partner is null) return false;

        var orders = await _unitOfWork.Orders.FindAsync(o => o.AffiliatePartnerId == id, cancellationToken);
        if (orders.Count > 0)
            throw new InvalidOperationException("Cannot delete partner with attributed orders. Deactivate instead.");

        await _unitOfWork.AffiliatePartners.DeleteAsync(partner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AffiliateLedgerDto?> GetLedgerAsync(int partnerId, CancellationToken cancellationToken = default)
    {
        var partner = await _unitOfWork.AffiliatePartners.GetByIdAsync(partnerId, cancellationToken);
        if (partner is null) return null;

        var partnerDto = await MapPartnerAsync(partner, cancellationToken);
        var entries = await _unitOfWork.AffiliateLedgers.GetByPartnerAsync(partnerId, cancellationToken);
        return new AffiliateLedgerDto(
            partnerDto,
            partnerDto.TotalCommission,
            partnerDto.TotalPaid,
            partnerDto.Balance,
            entries.Select(MapEntry).ToList());
    }

    public async Task<AffiliateLedgerDto?> GetLedgerByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var partner = await _unitOfWork.AffiliatePartners.GetByCodeAsync(code, cancellationToken);
        if (partner is null)
            return null;

        await MigrateLegacyCodeIfNeededAsync(partner, cancellationToken);
        var ledger = await GetLedgerAsync(partner.Id, cancellationToken);
        if (ledger is null)
            return null;

        // Public portal must not expose payout / contact details.
        var publicPartner = ledger.Partner with
        {
            Whatsapp = null,
            AccountNumber = null,
            WalletBankName = null
        };
        return ledger with { Partner = publicPartner };
    }

    public async Task<AffiliateLedgerEntryDto> RecordPaymentAsync(
        int partnerId, RecordAffiliatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var partner = await _unitOfWork.AffiliatePartners.GetByIdAsync(partnerId, cancellationToken)
            ?? throw new InvalidOperationException("Affiliate partner not found.");

        var amount = decimal.Round(request.Amount, 2);
        if (amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        var totals = await GetTotalsAsync(partnerId, cancellationToken);
        if (totals.Balance <= 0)
            throw new InvalidOperationException("No outstanding balance to pay.");

        if (amount > totals.Balance)
            throw new InvalidOperationException(
                $"Payment cannot exceed outstanding balance (Rs. {totals.Balance:N2}).");

        var entry = new AffiliateLedgerEntry
        {
            AffiliatePartnerId = partner.Id,
            Type = AffiliateLedgerEntryType.Payment,
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(request.Notes)
                ? "Payment to affiliate partner"
                : request.Notes.Trim()
        };

        await _unitOfWork.AffiliateLedgers.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapEntry(entry);
    }

    public async Task ApplyCommissionForDeliveredOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(orderId, cancellationToken);
        if (order is null || !order.AffiliatePartnerId.HasValue)
            return;

        if (order.Status != OrderStatus.Delivered)
            return;

        var activeCommission = await GetActiveCommissionForOrderAsync(orderId, cancellationToken);
        if (activeCommission > 0)
            return;

        var commissionBase = GetCommissionBase(order.SubTotal, order.DiscountAmount);
        if (commissionBase <= 0)
            return;

        var amount = CalculateCommission(order.SubTotal, order.DiscountAmount);
        if (amount <= 0)
            return;

        var entry = new AffiliateLedgerEntry
        {
            AffiliatePartnerId = order.AffiliatePartnerId.Value,
            Type = AffiliateLedgerEntryType.Commission,
            Amount = amount,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Description = $"10% commission on order {order.OrderNumber} (after discount, excl. delivery)"
        };

        await _unitOfWork.AffiliateLedgers.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReverseCommissionForOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var active = await GetActiveCommissionForOrderAsync(orderId, cancellationToken);
        if (active <= 0)
            return;

        var commission = await _unitOfWork.AffiliateLedgers.GetCommissionForOrderAsync(orderId, cancellationToken);
        if (commission is null)
            return;

        var entry = new AffiliateLedgerEntry
        {
            AffiliatePartnerId = commission.AffiliatePartnerId,
            Type = AffiliateLedgerEntryType.Reversal,
            Amount = active,
            OrderId = orderId,
            OrderNumber = commission.OrderNumber,
            Description = $"Commission reversed for order {commission.OrderNumber}"
        };

        await _unitOfWork.AffiliateLedgers.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<decimal> GetActiveCommissionForOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var earned = await _unitOfWork.AffiliateLedgers.SumByOrderAndTypeAsync(
            orderId, AffiliateLedgerEntryType.Commission, cancellationToken);
        var reversed = await _unitOfWork.AffiliateLedgers.SumByOrderAndTypeAsync(
            orderId, AffiliateLedgerEntryType.Reversal, cancellationToken);
        return earned - reversed;
    }

    private async Task<AffiliatePartnerDto> MapPartnerAsync(AffiliatePartner partner, CancellationToken cancellationToken)
    {
        var totals = await GetTotalsAsync(partner.Id, cancellationToken);
        var orderCount = (await _unitOfWork.Orders.FindAsync(o => o.AffiliatePartnerId == partner.Id, cancellationToken)).Count;
        return new AffiliatePartnerDto(
            partner.Id,
            partner.Name,
            partner.Whatsapp,
            partner.AccountNumber,
            partner.WalletBankName,
            partner.Code,
            partner.IsActive,
            partner.CreatedAt,
            totals.Commission,
            totals.Paid,
            totals.Balance,
            orderCount);
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private async Task<(decimal Commission, decimal Paid, decimal Balance)> GetTotalsAsync(
        int partnerId, CancellationToken cancellationToken)
    {
        var commission = await _unitOfWork.AffiliateLedgers.SumByPartnerAndTypeAsync(
            partnerId, AffiliateLedgerEntryType.Commission, cancellationToken);
        var paid = await _unitOfWork.AffiliateLedgers.SumByPartnerAndTypeAsync(
            partnerId, AffiliateLedgerEntryType.Payment, cancellationToken);
        var reversed = await _unitOfWork.AffiliateLedgers.SumByPartnerAndTypeAsync(
            partnerId, AffiliateLedgerEntryType.Reversal, cancellationToken);
        var netCommission = decimal.Round(commission - reversed, 2);
        var paidRounded = decimal.Round(paid, 2);
        return (netCommission, paidRounded, decimal.Round(netCommission - paidRounded, 2));
    }

    private static AffiliateLedgerEntryDto MapEntry(AffiliateLedgerEntry entry) =>
        new(entry.Id, entry.Type.ToString(), entry.Amount, entry.Description, entry.OrderId, entry.OrderNumber, entry.CreatedAt);

    private async Task MigrateLegacyCodeIfNeededAsync(AffiliatePartner partner, CancellationToken cancellationToken)
    {
        // Only fill missing/temp codes. Never rewrite an existing public affiliate code/link.
        if (!string.IsNullOrWhiteSpace(partner.Code)
            && !partner.Code.StartsWith("TMP", StringComparison.OrdinalIgnoreCase))
            return;

        partner.Code = await EnsureUniqueOpaqueCodeAsync(partner.Id, cancellationToken);
        await _unitOfWork.AffiliatePartners.UpdateAsync(partner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> EnsureUniqueOpaqueCodeAsync(int partnerId, CancellationToken cancellationToken)
    {
        var code = GenerateOpaqueCode(partnerId);
        var candidate = code;
        var suffix = 0;
        while (await _unitOfWork.AffiliatePartners.CodeExistsAsync(candidate, partnerId, cancellationToken))
        {
            suffix++;
            candidate = $"{code}{suffix}";
            if (candidate.Length > 50)
                candidate = $"{code[..Math.Max(1, 50 - suffix.ToString().Length)]}{suffix}";
        }

        return candidate;
    }

    /// <summary>
    /// Stable opaque referral token from partner Id — not reversible to a readable name.
    /// </summary>
    private static string GenerateOpaqueCode(int partnerId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"kidsparadise-aff-v1:{partnerId}"));
        return Convert.ToHexString(bytes)[..12];
    }
}
