using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

public class ToyService : IToyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public ToyService(IUnitOfWork unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    public async Task<PagedResult<ToyListDto>> GetAvailableAsync(
        int? categoryId, string? search, bool? onSale, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var toys = await _unitOfWork.Toys.GetAvailableAsync(categoryId, search, onSale, sort, page, pageSize, cancellationToken);
        var total = await _unitOfWork.Toys.CountAvailableAsync(categoryId, search, onSale, cancellationToken);
        var items = toys.Select(t => ToyMapper.MapList(t, t.Category?.Name ?? "", _fileStorage, null)).ToList();
        return new PagedResult<ToyListDto>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<ToyListDto>> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        var toys = await _unitOfWork.Toys.GetLatestAvailableAsync(8, cancellationToken);
        return toys.Select(t => ToyMapper.MapList(t, t.Category?.Name ?? "", _fileStorage, null)).ToList();
    }

    public async Task<ToyDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => MapDetail(await _unitOfWork.Toys.GetWithDetailsAsync(id, cancellationToken));

    public async Task<IReadOnlyList<ToyListDto>> GetAllAdminAsync(CancellationToken cancellationToken = default)
    {
        var toys = await _unitOfWork.Toys.GetAllAdminWithDetailsAsync(cancellationToken);
        return toys.Select(t => ToyMapper.MapList(t, t.Category?.Name ?? "", _fileStorage, null)).ToList();
    }

    public async Task<PagedResult<ToyListDto>> GetAdminPagedAsync(
        int? categoryId, string? search, bool? isSold, bool? onSale, string? sort,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var toys = await _unitOfWork.Toys.GetAdminPagedAsync(categoryId, search, isSold, onSale, sort, page, pageSize, cancellationToken);
        var total = await _unitOfWork.Toys.CountAdminAsync(categoryId, search, isSold, onSale, cancellationToken);
        var items = toys
            .Select(t => ToyMapper.MapListPrimaryImageOnly(t, t.Category?.Name ?? "", _fileStorage, null))
            .ToList();
        return new PagedResult<ToyListDto>(items, total, page, pageSize);
    }

    public async Task<ToyDetailDto?> GetByIdAdminAsync(int id, CancellationToken cancellationToken = default)
        => MapDetail(await _unitOfWork.Toys.GetWithDetailsAsync(id, cancellationToken));

    public async Task<ToyListDto> CreateAsync(CreateToyRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Toy
        {
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Price = request.Price,
            SalePrice = request.SalePrice,
            VideoLink = NormalizeVideoLink(request.VideoLink),
            Images = ToyMapper.BuildImages(0, request.ImagePaths)
        };
        await _unitOfWork.Toys.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var category = await _unitOfWork.Categories.GetByIdAsync(entity.CategoryId, cancellationToken);
        var withImages = await _unitOfWork.Toys.GetWithImagesAsync(entity.Id, cancellationToken) ?? entity;
        return ToyMapper.MapList(withImages, category?.Name ?? "", _fileStorage, null);
    }

    public async Task<ToyListDto> CloneAsync(int id, CancellationToken cancellationToken = default)
    {
        var source = await _unitOfWork.Toys.GetWithImagesAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Toy not found.");

        var imagePaths = new List<string>();
        foreach (var path in source.Images
                     .OrderBy(i => i.SortOrder)
                     .Select(i => i.ImagePath)
                     .Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            var copiedPath = await _fileStorage.CopyImageAsync(path, cancellationToken);
            if (!string.IsNullOrWhiteSpace(copiedPath))
                imagePaths.Add(copiedPath);
        }

        var entity = new Toy
        {
            CategoryId = source.CategoryId,
            Name = source.Name.Trim(),
            Price = source.Price,
            SalePrice = source.SalePrice,
            VideoLink = source.VideoLink,
            IsSold = false,
            Images = ToyMapper.BuildImages(0, imagePaths),
        };

        await _unitOfWork.Toys.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var category = await _unitOfWork.Categories.GetByIdAsync(entity.CategoryId, cancellationToken);
        var withImages = await _unitOfWork.Toys.GetWithImagesAsync(entity.Id, cancellationToken) ?? entity;
        return ToyMapper.MapList(withImages, category?.Name ?? "", _fileStorage, null);
    }

    public async Task<ToyListDto?> UpdateAsync(int id, UpdateToyRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Toys.GetWithImagesAsync(id, cancellationToken);
        if (entity is null) return null;

        entity.CategoryId = request.CategoryId;
        entity.Name = request.Name.Trim();
        entity.Price = request.Price;
        entity.SalePrice = request.SalePrice;
        entity.VideoLink = NormalizeVideoLink(request.VideoLink);

        var newPaths = request.ImagePaths?
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .ToList() ?? [];

        if (newPaths.Count > 0)
        {
            var keepPaths = newPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var old in entity.Images.ToList())
            {
                if (!keepPaths.Contains(old.ImagePath))
                {
                    _fileStorage.DeleteImage(old.ImagePath);
                    entity.Images.Remove(old);
                }
            }

            for (var index = 0; index < newPaths.Count; index++)
            {
                var path = newPaths[index];
                var existing = entity.Images.FirstOrDefault(i =>
                    string.Equals(i.ImagePath, path, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                    existing.SortOrder = index;
                else
                    entity.Images.Add(new ToyImage { ToyId = entity.Id, ImagePath = path, SortOrder = index });
            }
        }

        await _unitOfWork.Toys.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var category = await _unitOfWork.Categories.GetByIdAsync(entity.CategoryId, cancellationToken);
        return ToyMapper.MapList(entity, category?.Name ?? "", _fileStorage, null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Toys.GetWithImagesAsync(id, cancellationToken);
        if (entity is null) return false;

        foreach (var img in entity.Images)
            _fileStorage.DeleteImage(img.ImagePath);
        await _unitOfWork.Toys.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private ToyDetailDto? MapDetail(Toy? toy)
    {
        if (toy is null) return null;

        var reviews = toy.Reviews.ToList();
        var avg = reviews.Count > 0 ? reviews.Average(r => r.Rating) : (double?)null;

        var imagePaths = toy.Images.OrderBy(i => i.SortOrder).Select(i => i.ImagePath).ToList();

        return new ToyDetailDto(
            toy.Id, toy.Name, toy.Price, toy.SalePrice, toy.IsSold,
            imagePaths, ToyMapper.ImageUrls(toy, _fileStorage), toy.Category?.Name ?? "", toy.CategoryId,
            avg, reviews.Count, toy.VideoLink);
    }

    private static string? NormalizeVideoLink(string? videoLink)
    {
        if (string.IsNullOrWhiteSpace(videoLink))
            return null;

        return videoLink.Trim();
    }
}
