namespace KidsParadiseByShoptick.Domain.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ToyId { get; set; }

    public Order Order { get; set; } = null!;
    public Toy Toy { get; set; } = null!;
}
