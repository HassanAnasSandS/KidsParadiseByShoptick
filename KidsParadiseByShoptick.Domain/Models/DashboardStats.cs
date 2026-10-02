namespace KidsParadiseByShoptick.Domain.Models;

public class DashboardStats
{
    public int TotalToys { get; set; }
    public int TotalAvailableToys { get; set; }
    public int TotalSoldToys { get; set; }
    public int TotalToysOnSale { get; set; }
    public int TotalToysOnRegular { get; set; }
    public decimal RegularToysTotalAmount { get; set; }
    public decimal OnSaleToysTotalAmount { get; set; }
    public decimal AllToysTotalAmount { get; set; }
    public decimal AvailableToysTotalAmount { get; set; }
    public decimal AllSoldToysTotalAmount { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalDeliveredOrders { get; set; }
    public decimal AllDeliveredOrdersTotalAmount { get; set; }

    // Affiliate (informational only — not deducted from sales/revenue)
    public int TotalAffiliatePartners { get; set; }
    public int ActiveAffiliatePartners { get; set; }
    public int AffiliatedOrdersCount { get; set; }
    public decimal AffiliateCommissionTotal { get; set; }
    public decimal AffiliatePaidTotal { get; set; }
    public decimal AffiliateOutstandingTotal { get; set; }
    public IReadOnlyList<DashboardAffiliatePartnerStat> AffiliatePartners { get; set; } = [];
}

public class DashboardAffiliatePartnerStat
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int AttributedOrders { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
}
