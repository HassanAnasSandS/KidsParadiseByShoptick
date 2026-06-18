namespace KidsParadiseByShoptick.Domain.Entities;

public class Toy : BaseEntity
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public bool IsSold { get; set; }

    public ToyCategory Category { get; set; } = null!;
    public ICollection<ToyImage> Images { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];

    public decimal EffectivePrice => SalePrice ?? Price;
}
