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
}
