using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace KidsParadiseByShoptick.Infrastructure.Services;

public sealed class TikTokPhotoPrepareService : ITikTokPhotoPrepareService
{
    // TikTok: "Maximum 1080p" — portrait 1080×1920 / landscape 1920×1080.
    private const int MaxShortSide = 1080;
    private const int MaxLongSide = 1920;
    private const int MaxImages = 12;
    private const int JpegQuality = 85;

    private readonly IFileStorageService _files;
    private readonly ILogger<TikTokPhotoPrepareService> _logger;

    public TikTokPhotoPrepareService(IFileStorageService files, ILogger<TikTokPhotoPrepareService> logger)
    {
        _files = files;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> PreparePublicJpegUrlsAsync(
        int toyId,
        IEnumerable<string> sourceRelativePaths,
        string siteBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var siteBase = (siteBaseUrl ?? string.Empty).TrimEnd('/');
        var urls = new List<string>();
        var index = 0;

        foreach (var relative in sourceRelativePaths
                     .Where(p => !string.IsNullOrWhiteSpace(p))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Take(MaxImages))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sourcePath = _files.GetAbsolutePath(relative);
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                _logger.LogWarning("TikTok photo skip missing file {Path}", relative);
                continue;
            }

            try
            {
                var destRelative = $"uploads/tiktok-pull/{toyId}-{index}.jpg";
                var destPath = _files.GetAbsolutePath(destRelative);
                if (string.IsNullOrWhiteSpace(destPath))
                    continue;

                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                await WriteTikTokJpegAsync(sourcePath, destPath, cancellationToken);
                urls.Add($"{siteBase}/{destRelative}");
                index++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TikTok photo prepare failed for {Path}", relative);
            }
        }

        return urls;
    }

    static async Task WriteTikTokJpegAsync(string sourcePath, string destPath, CancellationToken cancellationToken)
    {
        await using var input = File.OpenRead(sourcePath);
        using var image = await Image.LoadAsync(input, cancellationToken);
        image.Mutate(x => x.AutoOrient());

        var maxWidth = image.Width >= image.Height ? MaxLongSide : MaxShortSide;
        var maxHeight = image.Width >= image.Height ? MaxShortSide : MaxLongSide;
        if (image.Width > maxWidth || image.Height > maxHeight)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight),
                Sampler = KnownResamplers.Lanczos3,
            }));
        }

        var encoder = new JpegEncoder { Quality = JpegQuality };
        await image.SaveAsJpegAsync(destPath, encoder, cancellationToken);
    }
}
