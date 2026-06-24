using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<CategoryDto>> GetPublicPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CategoryDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetAllAdminAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<CategoryDto>> GetAdminPagedAsync(
        string? search, string? toyFilter, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
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
    Task<PagedResult<ToyListDto>> GetAdminPagedAsync(
        int? categoryId, string? search, bool? isSold, bool? onSale, string? sort,
        int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ToyDetailDto?> GetByIdAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<ToyListDto> CreateAsync(CreateToyRequest request, CancellationToken cancellationToken = default);
    Task<ToyListDto?> UpdateAsync(int id, UpdateToyRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IOrderService
{
    Task<OrderPlacedDto> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetOrdersByWhatsappAsync(string whatsapp, CancellationToken cancellationToken = default);
    Task<PagedResult<OrderDto>> GetOrdersByWhatsappPagedAsync(
        string whatsapp, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetAllAdminAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<OrderDto>> GetAdminPagedAsync(
        string? status, string? search, string? city, DateTime? dateFrom, DateTime? dateTo, string? sort,
        int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAdminCitiesAsync(CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByIdAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderDto?> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto?> UpdateAdminAsync(int id, AdminUpdateOrderRequest request, CancellationToken cancellationToken = default);
}

public interface IReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ReviewDto>> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewDto>> GetByToyIdAsync(int toyId, CancellationToken cancellationToken = default);
    Task<PagedResult<ReviewDto>> GetByToyIdPagedAsync(
        int toyId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingReviewDto>> GetPendingForCustomerAsync(string whatsapp, CancellationToken cancellationToken = default);
    Task<PagedResult<PendingReviewDto>> GetPendingForCustomerPagedAsync(
        string whatsapp, int page, int pageSize, CancellationToken cancellationToken = default);
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
