using KidsParadiseByShoptick.Domain.Enums;

namespace KidsParadiseByShoptick.Domain.Entities;

public class AffiliateLedgerEntry : BaseEntity
{
    public int AffiliatePartnerId { get; set; }
    public AffiliateLedgerEntryType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public int? OrderId { get; set; }
    public string? OrderNumber { get; set; }

    public AffiliatePartner AffiliatePartner { get; set; } = null!;
    public Order? Order { get; set; }
}
