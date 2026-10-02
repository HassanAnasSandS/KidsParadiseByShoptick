namespace KidsParadiseByShoptick.Domain.Entities;

public class AffiliatePartner : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Whatsapp { get; set; }
    public string? AccountNumber { get; set; }
    public string? WalletBankName { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<AffiliateLedgerEntry> LedgerEntries { get; set; } = [];
}
