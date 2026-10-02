using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

public class SiteImageService : ISiteImageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public SiteImageService(IUnitOfWork unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetPublicUrlsAsync(CancellationToken cancellationToken = default)
    {
        var images = await _unitOfWork.SiteImages.GetAllOrderedAsync(cancellationToken);
        return images.ToDictionary(x => x.Key, ResolveUrl);
    }

    public async Task<IReadOnlyDictionary<string, SiteImagePublicDto>> GetPublicContentAsync(
        CancellationToken cancellationToken = default)
    {
        var images = await _unitOfWork.SiteImages.GetAllOrderedAsync(cancellationToken);
        return images.ToDictionary(x => x.Key, MapPublic);
    }

    public async Task<IReadOnlyList<SiteImageAdminDto>> GetAdminAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetAdminPagedAsync(1, 100, cancellationToken);
        return result.Items;
    }

    public async Task<PagedResult<SiteImageAdminDto>> GetAdminPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var images = await _unitOfWork.SiteImages.GetAllOrderedAsync(cancellationToken);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var all = images.Select(MapAdmin).ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<SiteImageAdminDto>(items, all.Count, page, pageSize);
    }

    public async Task<SiteImageAdminDto> UploadAsync(
        string key, Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var image = await _unitOfWork.SiteImages.GetByKeyAsync(key, cancellationToken)
            ?? throw new InvalidOperationException("Image slot not found.");

        if (!string.IsNullOrWhiteSpace(image.ImagePath))
            _fileStorage.DeleteImage(image.ImagePath);

        var path = await _fileStorage.SaveImageAsync(fileStream, fileName, "site", cancellationToken);
        image.ImagePath = path;
        await _unitOfWork.SiteImages.UpdateAsync(image, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapAdmin(image);
    }

    public async Task<SiteImageAdminDto> UpdateContentAsync(
        string key, UpdateSiteImageContentRequest request, CancellationToken cancellationToken = default)
    {
        var image = await _unitOfWork.SiteImages.GetByKeyAsync(key, cancellationToken)
            ?? throw new InvalidOperationException("Image slot not found.");

        var def = SiteImageDefaults.GetDefinition(key);
        if (def is null || !def.SupportsText)
            throw new InvalidOperationException("This image slot does not support text content.");

        image.Title = Normalize(request.Title, 200);
        image.Subtitle = Normalize(request.Subtitle, 500);
        image.CtaText = Normalize(request.CtaText, 100);
        image.LinkUrl = Normalize(request.LinkUrl, 300);
        image.TitleColor = NormalizeColor(request.TitleColor);
        image.SubtitleColor = NormalizeColor(request.SubtitleColor);
        image.CtaColor = NormalizeColor(request.CtaColor);

        await _unitOfWork.SiteImages.UpdateAsync(image, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapAdmin(image);
    }

    public async Task<SiteImageAdminDto> ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        var image = await _unitOfWork.SiteImages.GetByKeyAsync(key, cancellationToken)
            ?? throw new InvalidOperationException("Image slot not found.");

        if (!string.IsNullOrWhiteSpace(image.ImagePath))
        {
            _fileStorage.DeleteImage(image.ImagePath);
            image.ImagePath = null;
            await _unitOfWork.SiteImages.UpdateAsync(image, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return MapAdmin(image);
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = SiteImageDefaults.NormalizeHexColor(value)
            ?? throw new InvalidOperationException(
                "Invalid color. Use hex like #FFFFFF, #FFF, or #FFFFFFCC.");
        return normalized;
    }

    private string ResolveUrl(Domain.Entities.SiteImage image)
    {
        if (!string.IsNullOrWhiteSpace(image.ImagePath))
            return _fileStorage.GetPublicUrl(image.ImagePath);
        return SiteImageDefaults.GetDefaultUrl(image.Key);
    }

    private static string? ResolveText(string? stored)
        => string.IsNullOrWhiteSpace(stored) ? null : stored.Trim();

    private static string? ResolveColor(string? stored)
        => SiteImageDefaults.NormalizeHexColor(stored);

    private SiteImagePublicDto MapPublic(Domain.Entities.SiteImage image)
    {
        return new SiteImagePublicDto(
            ResolveUrl(image),
            ResolveText(image.Title),
            ResolveText(image.Subtitle),
            ResolveText(image.CtaText),
            ResolveText(image.LinkUrl),
            ResolveColor(image.TitleColor),
            ResolveColor(image.SubtitleColor),
            ResolveColor(image.CtaColor));
    }

    private SiteImageAdminDto MapAdmin(Domain.Entities.SiteImage image)
    {
        var def = SiteImageDefaults.GetDefinition(image.Key);
        var defaultUrl = SiteImageDefaults.GetDefaultUrl(image.Key);
        var imageUrl = !string.IsNullOrWhiteSpace(image.ImagePath)
            ? _fileStorage.GetPublicUrl(image.ImagePath)
            : defaultUrl;

        return new SiteImageAdminDto(
            image.Key,
            image.Label,
            image.Group,
            image.SortOrder,
            imageUrl,
            defaultUrl,
            !string.IsNullOrWhiteSpace(image.ImagePath),
            ResolveText(image.Title),
            ResolveText(image.Subtitle),
            ResolveText(image.CtaText),
            ResolveText(image.LinkUrl),
            ResolveColor(image.TitleColor),
            ResolveColor(image.SubtitleColor),
            ResolveColor(image.CtaColor),
            def?.SupportsText == true);
    }
}
