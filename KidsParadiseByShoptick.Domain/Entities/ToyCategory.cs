namespace KidsParadiseByShoptick.Domain.Entities;

public class ToyCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ImagePath { get; set; }

    public ICollection<Toy> Toys { get; set; } = [];
}
