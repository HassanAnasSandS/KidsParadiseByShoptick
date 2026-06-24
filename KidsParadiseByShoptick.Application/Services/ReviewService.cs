using KidsParadiseByShoptick.Application;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Enums;
using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public ReviewService(IUnitOfWork unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyList<ReviewDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetPagedAsync(null, 1, int.MaxValue, cancellationToken);
        return result.Items;
    }

    public async Task<PagedResult<ReviewDto>> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var reviews = await _unitOfWork.Reviews.GetPagedAsync(search, page, pageSize, cancellationToken);
        var total = await _unitOfWork.Reviews.CountAsync(search, cancellationToken);
        return new PagedResult<ReviewDto>(reviews.Select(Map).ToList(), total, page, pageSize);
    }

    public async Task<IReadOnlyList<ReviewDto>> GetByToyIdAsync(int toyId, CancellationToken cancellationToken = default)
    {
        var result = await GetByToyIdPagedAsync(toyId, 1, int.MaxValue, cancellationToken);
        return result.Items;
    }

    public async Task<PagedResult<ReviewDto>> GetByToyIdPagedAsync(
        int toyId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var reviews = await _unitOfWork.Reviews.GetByToyIdPagedAsync(toyId, page, pageSize, cancellationToken);
        var total = await _unitOfWork.Reviews.CountByToyIdAsync(toyId, cancellationToken);
        return new PagedResult<ReviewDto>(reviews.Select(Map).ToList(), total, page, pageSize);
    }

    public async Task<IReadOnlyList<PendingReviewDto>> GetPendingForCustomerAsync(string whatsapp, CancellationToken cancellationToken = default)
    {
        var customer = await _unitOfWork.Customers.GetByWhatsappAsync(whatsapp, cancellationToken);
        if (customer is null) return [];

        var orders = await _unitOfWork.Orders.GetDeliveredOrdersForCustomerAsync(customer.Id, cancellationToken);
        var pending = new List<PendingReviewDto>();

        foreach (var order in orders)
        {
            foreach (var item in order.Items)
            {
                if (await _unitOfWork.Reviews.ExistsForOrderAndToyAsync(order.Id, item.ToyId, cancellationToken))
                    continue;

                var imagePath = item.Toy.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImagePath;
                pending.Add(new PendingReviewDto(
                    order.Id,
                    order.OrderNumber,
                    item.ToyId,
                    item.Toy.Name,
                    string.IsNullOrWhiteSpace(imagePath) ? null : _fileStorage.GetPublicUrl(imagePath)));
            }
        }

        return pending;
    }

    public async Task<PagedResult<PendingReviewDto>> GetPendingForCustomerPagedAsync(
        string whatsapp, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = await GetPendingForCustomerAsync(whatsapp, cancellationToken);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<PendingReviewDto>(items, all.Count, page, pageSize);
    }

    public async Task<ReviewDto> CreateAsync(CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");

        var customer = await _unitOfWork.Customers.GetByWhatsappAsync(request.Whatsapp, cancellationToken)
            ?? throw new InvalidOperationException("No delivered order found for this WhatsApp number.");

        var order = await _unitOfWork.Orders.GetWithDetailsAsync(request.OrderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.CustomerId != customer.Id)
            throw new InvalidOperationException("This order does not belong to your account.");

        if (order.Status != OrderStatus.Delivered)
            throw new InvalidOperationException("You can only review after your order is delivered.");

        if (!order.Items.Any(i => i.ToyId == request.ToyId))
            throw new InvalidOperationException("This toy is not part of the selected order.");

        if (await _unitOfWork.Reviews.ExistsForOrderAndToyAsync(request.OrderId, request.ToyId, cancellationToken))
            throw new InvalidOperationException("You have already reviewed this item from this order.");

        var review = new Review
        {
            OrderId = request.OrderId,
            ToyId = request.ToyId,
            CustomerId = customer.Id,
            ReviewerName = request.ReviewerName.Trim(),
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            ImagePath = string.IsNullOrWhiteSpace(request.ImagePath) ? null : request.ImagePath.Trim()
        };

        await _unitOfWork.Reviews.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _unitOfWork.Reviews.GetWithDetailsAsync(review.Id, cancellationToken) ?? review;
        return Map(saved);
    }

    public async Task<IReadOnlyList<ReviewDto>> GetAllAdminAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetPagedAsync(null, 1, int.MaxValue, cancellationToken);
        return result.Items;
    }

    public async Task<ReviewDto?> UpdateAdminAsync(int id, AdminUpdateReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");

        var review = await _unitOfWork.Reviews.GetWithDetailsAsync(id, cancellationToken);
        if (review is null) return null;

        review.ReviewerName = request.ReviewerName.Trim();
        review.Rating = request.Rating;
        review.Comment = request.Comment.Trim();

        if (request.ImagePath is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ImagePath))
            {
                _fileStorage.DeleteImage(review.ImagePath);
                review.ImagePath = null;
            }
            else if (review.ImagePath != request.ImagePath.Trim())
            {
                _fileStorage.DeleteImage(review.ImagePath);
                review.ImagePath = request.ImagePath.Trim();
            }
        }

        await _unitOfWork.Reviews.UpdateAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(review);
    }

    private ReviewDto Map(Review r) => new(
        r.Id,
        r.ReviewerName,
        r.Rating,
        r.Comment,
        string.IsNullOrWhiteSpace(r.ImagePath) ? null : _fileStorage.GetPublicUrl(r.ImagePath),
        string.IsNullOrWhiteSpace(r.ImagePath) ? null : r.ImagePath,
        r.Toy?.Name ?? "",
        r.ToyId,
        r.Order?.OrderNumber ?? "",
        r.CreatedAt);
}
