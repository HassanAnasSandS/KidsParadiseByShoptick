namespace KidsParadiseByShoptick.Application.Interfaces;

/// <summary>
/// Free, offline image fingerprinting: perceptual hash (exact/near-exact)
/// + CLIP vision embeddings (same product, different photo).
/// </summary>
public interface IImageFingerprintService
{
    int EmbeddingDimensions { get; }
    bool IsEmbeddingReady { get; }

    Task EnsureEmbeddingModelAsync(CancellationToken cancellationToken = default);

    long ComputePerceptualHash(Stream imageStream);
    long ComputePerceptualHashFromFile(string absolutePath);

    Task<float[]?> ComputeEmbeddingAsync(Stream imageStream, CancellationToken cancellationToken = default);
    Task<float[]?> ComputeEmbeddingFromFileAsync(string absolutePath, CancellationToken cancellationToken = default);

    static int HammingDistance(long a, long b)
    {
        var x = (ulong)a ^ (ulong)b;
        var count = 0;
        while (x != 0)
        {
            x &= x - 1;
            count++;
        }
        return count;
    }

    static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length == 0 || a.Length != b.Length) return 0f;
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        if (na <= 0 || nb <= 0) return 0f;
        return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb)));
    }

    static byte[] EmbeddingToBytes(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    static float[]? BytesToEmbedding(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0 || bytes.Length % sizeof(float) != 0)
            return null;
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
