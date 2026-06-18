using KidsParadiseByShoptick.Domain.Enums;

namespace KidsParadiseByShoptick.Domain.Entities;

public class Order : BaseEntity
{
    public int CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DeliveryCharge { get; set; }
    public decimal Total { get; set; }
    public decimal? AdvanceAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Whatsapp { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
}
