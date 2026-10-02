using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace KidsParadiseByShoptick.Application.Services;

public class ToyImageSearchService : IToyImageSearchService
{
    // Hamming distance ≤ this = near-exact (same/edited upload).
    private const int ExactHashMaxDistance = 8;
    // Cosine similarity ≥ this = plausible same-product match (different photo).
    private const float EmbeddingMinScore = 0.78f;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly IImageFingerprintService _fingerprint;
    private readonly ILogger<ToyImageSearchService> _logger;

    public ToyImageSearchService(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage,
        IImageFingerprintService fingerprint,
        ILogger<ToyImageSearchService> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _fingerprint = fingerprint;
        _logger = logger;
    }

    public async Task IndexToyAsync(int toyId, CancellationToken cancellationToken = default)
    {
        var toy = await _unitOfWork.Toys.GetWithImagesAsync(toyId, cancellationToken);
        if (toy is null) return;

        await _fingerprint.EnsureEmbeddingModelAsync(cancellationToken);
        var changed = false;

        foreach (var image in toy.Images.OrderBy(i => i.SortOrder))
        {
            if (await TryIndexImageAsync(image, force: false, cancellationToken))
                changed = true;
        }

        if (changed)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ToyImageIndexResultDto> BackfillAsync(CancellationToken cancellationToken = default)
    {
        await _fingerprint.EnsureEmbeddingModelAsync(cancellationToken);

        var toys = await _unitOfWork.Toys.GetAllWithImagesTrackedAsync(cancellationToken);
        var indexed = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var toy in toys)
        {
            foreach (var image in toy.Images)
            {
                try
                {
                    var needsWork = image.PerceptualHash is null
                        || (image.Embedding is null && _fingerprint.IsEmbeddingReady);
                    if (!needsWork)
                    {
                        skipped++;
                        continue;
                    }

                    if (await TryIndexImageAsync(image, force: false, cancellationToken))
                        indexed++;
                    else
                        failed++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogWarning(ex, "Failed to index toy image {ImageId} ({Path})", image.Id, image.ImagePath);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var clipNote = _fingerprint.IsEmbeddingReady
            ? "CLIP embeddings ready."
            : "CLIP model not ready yet — perceptual hashes indexed; embeddings will fill after the free model downloads.";

        return new ToyImageIndexResultDto(
            indexed, skipped, failed,
            $"Indexed {indexed}, skipped {skipped}, failed {failed}. {clipNote}");
    }

    public async Task<IReadOnlyList<ToyImageSearchMatchDto>> SearchAsync(
        Stream imageStream, int limit = 20, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        // Need a seekable copy — stream may be non-seekable multipart.
        await using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        long probeHash;
        try
        {
            probeHash = _fingerprint.ComputePerceptualHash(ms);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not read the uploaded image.", ex);
        }

        ms.Position = 0;
        await _fingerprint.EnsureEmbeddingModelAsync(cancellationToken);
        float[]? probeEmbedding = null;
        try
        {
            probeEmbedding = await _fingerprint.ComputeEmbeddingAsync(ms, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CLIP embedding failed for probe image; falling back to perceptual hash only.");
        }

        var toys = await _unitOfWork.Toys.GetAllWithImagesForSearchAsync(cancellationToken);
        var bestByToy = new Dictionary<int, (Toy Toy, double Score, string MatchType)>();

        foreach (var toy in toys)
        {
            foreach (var image in toy.Images)
            {
                double score = 0;
                var matchType = "none";

                if (image.PerceptualHash is long storedHash)
                {
                    var distance = IImageFingerprintService.HammingDistance(probeHash, storedHash);
                    if (distance <= ExactHashMaxDistance)
                    {
                        // Map distance 0→1.0, ExactHashMaxDistance→~0.85
                        var hashScore = 1.0 - (distance / 64.0);
                        if (hashScore > score)
                        {
                            score = hashScore;
                            matchType = distance == 0 ? "exact" : "near-exact";
                        }
                    }
                }

                if (probeEmbedding is not null)
                {
                    var stored = IImageFingerprintService.BytesToEmbedding(image.Embedding);
                    if (stored is not null)
                    {
                        var cos = IImageFingerprintService.CosineSimilarity(probeEmbedding, stored);
                        if (cos >= EmbeddingMinScore && cos > score)
                        {
                            score = cos;
                            matchType = "similar";
                        }
                    }
                }

                if (score <= 0)
                    continue;

                if (!bestByToy.TryGetValue(toy.Id, out var existing) || score > existing.Score)
                    bestByToy[toy.Id] = (toy, score, matchType);
            }
        }

        return bestByToy.Values
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => new ToyImageSearchMatchDto(
                x.Toy.Id,
                x.Toy.Name,
                x.Toy.Price,
                x.Toy.SalePrice,
                x.Toy.IsSold,
                ToyMapper.ImageUrls(x.Toy, _fileStorage),
                x.Toy.Category?.Name ?? "",
                Math.Round(x.Score, 4),
                x.MatchType))
            .ToList();
    }

    private async Task<bool> TryIndexImageAsync(ToyImage image, bool force, CancellationToken cancellationToken)
    {
        var absolute = _fileStorage.GetAbsolutePath(image.ImagePath);
        if (string.IsNullOrWhiteSpace(absolute) || !File.Exists(absolute))
            return false;

        var changed = false;

        if (force || image.PerceptualHash is null)
        {
            try
            {
                image.PerceptualHash = _fingerprint.ComputePerceptualHashFromFile(absolute);
                changed = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Perceptual hash failed for {Path}", image.ImagePath);
            }
        }

        if ((force || image.Embedding is null) && (_fingerprint.IsEmbeddingReady || force))
        {
            try
            {
                var embedding = await _fingerprint.ComputeEmbeddingFromFileAsync(absolute, cancellationToken);
                if (embedding is not null)
                {
                    image.Embedding = IImageFingerprintService.EmbeddingToBytes(embedding);
                    changed = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CLIP embedding failed for {Path}", image.ImagePath);
            }
        }

        return changed;
    }
}
