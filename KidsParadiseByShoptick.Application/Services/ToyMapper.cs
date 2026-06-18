using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

internal static class ToyMapper
{
    public static IReadOnlyList<string> ImageUrls(Toy toy, IFileStorageService fileStorage) =>
        toy.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => fileStorage.GetPublicUrl(i.ImagePath))
            .ToList();

    public static ToyListDto MapList(Toy t, string categoryName, IFileStorageService fileStorage, double? avg) =>
        new(t.Id, t.Name, t.Price, t.SalePrice, t.IsSold, ImageUrls(t, fileStorage), categoryName, avg);

    public static List<ToyImage> BuildImages(int toyId, IReadOnlyList<string> paths) =>
        paths.Select((path, index) => new ToyImage
        {
            ToyId = toyId,
            ImagePath = path,
            SortOrder = index
        }).ToList();
}
