namespace KidsParadiseByShoptick.Application.Interfaces;

/// <summary>
/// TikTok photo posts only accept JPEG/WebP at max 1080p via PULL_FROM_URL.
/// Prepares public HTTPS JPEG URLs that meet those limits.
/// </summary>
public interface ITikTokPhotoPrepareService
{
    Task<IReadOnlyList<string>> PreparePublicJpegUrlsAsync(
        int toyId,
        IEnumerable<string> sourceRelativePaths,
        string siteBaseUrl,
        CancellationToken cancellationToken = default);
}
