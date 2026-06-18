namespace KidsParadiseByShoptick.Domain.Entities;

public class Review : BaseEntity
{
    public int OrderId { get; set; }
    public int ToyId { get; set; }
    public int CustomerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? ImagePath { get; set; }

    public Order Order { get; set; } = null!;
    public Toy Toy { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}
