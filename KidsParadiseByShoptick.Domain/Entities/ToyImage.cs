namespace KidsParadiseByShoptick.Domain.Entities;

public class ToyImage : BaseEntity
{
    public int ToyId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Toy Toy { get; set; } = null!;
}
