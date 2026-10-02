using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ISiteImageService
{
    Task<IReadOnlyDictionary<string, string>> GetPublicUrlsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, SiteImagePublicDto>> GetPublicContentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteImageAdminDto>> GetAdminAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<SiteImageAdminDto>> GetAdminPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SiteImageAdminDto> UploadAsync(string key, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<SiteImageAdminDto> UpdateContentAsync(string key, UpdateSiteImageContentRequest request, CancellationToken cancellationToken = default);
    Task<SiteImageAdminDto> ResetAsync(string key, CancellationToken cancellationToken = default);
}

public record SiteImagePublicDto(
    string ImageUrl,
    string? Title,
    string? Subtitle,
    string? CtaText,
    string? LinkUrl,
    string? TitleColor,
    string? SubtitleColor,
    string? CtaColor);

public record SiteImageAdminDto(
    string Key,
    string Label,
    string Group,
    int SortOrder,
    string ImageUrl,
    string DefaultUrl,
    bool IsCustom,
    string? Title,
    string? Subtitle,
    string? CtaText,
    string? LinkUrl,
    string? TitleColor,
    string? SubtitleColor,
    string? CtaColor,
    bool SupportsText);

public record UpdateSiteImageContentRequest(
    string? Title,
    string? Subtitle,
    string? CtaText,
    string? LinkUrl,
    string? TitleColor,
    string? SubtitleColor,
    string? CtaColor);
