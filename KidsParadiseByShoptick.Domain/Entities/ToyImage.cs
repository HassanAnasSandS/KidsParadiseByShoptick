namespace KidsParadiseByShoptick.Domain.Entities;

public class ToyImage : BaseEntity
{
    public int ToyId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>64-bit perceptual (difference) hash for near-exact image match. Free, offline.</summary>
    public long? PerceptualHash { get; set; }

    /// <summary>L2-normalized CLIP vision embedding (float32 little-endian). Free ONNX model.</summary>
    public byte[]? Embedding { get; set; }

    public Toy Toy { get; set; } = null!;
}
