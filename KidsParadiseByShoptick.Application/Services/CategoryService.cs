using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public CategoryService(IUnitOfWork unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllOrderedAsync(cancellationToken);
        var allToys = await _unitOfWork.Toys.FindAsync(t => !t.IsSold, cancellationToken);
        var counts = allToys.GroupBy(t => t.CategoryId).ToDictionary(g => g.Key, g => g.Count());
        return categories.Select(c => Map(c, counts.GetValueOrDefault(c.Id, 0))).ToList();
    }

    public async Task<CategoryDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdWithToysAsync(id, cancellationToken);
        if (category is null) return null;

        var toys = category.Toys
            .Where(t => !t.IsSold)
            .Select(t => ToyMapper.MapList(t, category.Name, _fileStorage, null))
            .ToList();

        return new CategoryDetailDto(category.Id, category.Name, ToUrl(category.ImagePath), toys);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAdminAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllOrderedAsync(cancellationToken);
        var result = new List<CategoryDto>();
        foreach (var c in categories)
        {
            var toys = await _unitOfWork.Toys.FindAsync(t => t.CategoryId == c.Id, cancellationToken);
            result.Add(Map(c, toys.Count));
        }
        return result;
    }

    public async Task<CategoryDto?> GetByIdAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category is null) return null;
        var toys = await _unitOfWork.Toys.FindAsync(t => t.CategoryId == id, cancellationToken);
        return Map(category, toys.Count);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new ToyCategory
        {
            Name = request.Name.Trim(),
            ImagePath = request.ImagePath
        };
        await _unitOfWork.Categories.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity, 0);
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        entity.Name = request.Name.Trim();
        if (request.ImagePath is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ImagePath))
            {
                _fileStorage.DeleteImage(entity.ImagePath);
                entity.ImagePath = null;
            }
            else if (!string.Equals(entity.ImagePath, request.ImagePath.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _fileStorage.DeleteImage(entity.ImagePath);
                entity.ImagePath = request.ImagePath.Trim();
            }
        }
        await _unitOfWork.Categories.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var toys = await _unitOfWork.Toys.FindAsync(t => t.CategoryId == id, cancellationToken);
        return Map(entity, toys.Count);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (entity is null) return false;

        var toys = await _unitOfWork.Toys.FindAsync(t => t.CategoryId == id, cancellationToken);
        if (toys.Count > 0)
            throw new InvalidOperationException("Cannot delete category with toys.");

        _fileStorage.DeleteImage(entity.ImagePath);
        await _unitOfWork.Categories.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private CategoryDto Map(ToyCategory c, int toyCount) =>
        new(c.Id, c.Name, ToUrl(c.ImagePath), c.ImagePath, toyCount);

    private string? ToUrl(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : _fileStorage.GetPublicUrl(path);
}
