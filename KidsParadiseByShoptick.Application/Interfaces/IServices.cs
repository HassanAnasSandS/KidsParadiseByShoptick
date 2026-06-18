using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetAllAdminAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetByIdAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IToyService
{
    Task<PagedResult<ToyListDto>> GetAvailableAsync(
        int? categoryId, string? search, bool? onSale, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToyListDto>> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<ToyDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToyListDto>> GetAllAdminAsync(CancellationToken cancellationToken = default);
    Task<ToyDetailDto?> GetByIdAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<ToyListDto> CreateAsync(CreateToyRequest request, CancellationToken cancellationToken = default);
    Task<ToyListDto?> UpdateAsync(int id, UpdateToyRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IOrderService
{
    Task<OrderPlacedDto> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetOrdersByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetAllAdminAsync(CancellationToken cancellationToken = default);
    Task<OrderDto?> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
}

public interface IReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewDto>> GetByToyIdAsync(int toyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingReviewDto>> GetPendingForCustomerAsync(string email, CancellationToken cancellationToken = default);
    Task<ReviewDto> CreateAsync(CreateReviewRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewDto>> GetAllAdminAsync(CancellationToken cancellationToken = default);
    Task<ReviewDto?> UpdateAdminAsync(int id, AdminUpdateReviewRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminAuthService
{
    Task<AdminLoginResponse?> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken = default);
}

public interface IDeliveryChargeService
{
    decimal Calculate(string city);
}
