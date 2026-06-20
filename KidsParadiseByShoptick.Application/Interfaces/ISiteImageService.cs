namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ISiteImageService
{
    Task<IReadOnlyDictionary<string, string>> GetPublicUrlsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteImageAdminDto>> GetAdminAsync(CancellationToken cancellationToken = default);
    Task<SiteImageAdminDto> UploadAsync(string key, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<SiteImageAdminDto> ResetAsync(string key, CancellationToken cancellationToken = default);
}

public record SiteImageAdminDto(
    string Key,
    string Label,
    string Group,
    int SortOrder,
    string ImageUrl,
    string DefaultUrl,
    bool IsCustom);
