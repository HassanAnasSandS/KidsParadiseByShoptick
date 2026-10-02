using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface IToyImageSearchService
{
    /// <summary>Index missing perceptual hash + CLIP embedding for a toy's images.</summary>
    Task IndexToyAsync(int toyId, CancellationToken cancellationToken = default);

    /// <summary>Backfill fingerprints for every toy image that is missing them.</summary>
    Task<ToyImageIndexResultDto> BackfillAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search toys by a probe image. Combines perceptual-hash (exact/near match)
    /// with CLIP cosine similarity (same toy, different photo).
    /// </summary>
    Task<IReadOnlyList<ToyImageSearchMatchDto>> SearchAsync(
        Stream imageStream, int limit = 20, CancellationToken cancellationToken = default);
}
