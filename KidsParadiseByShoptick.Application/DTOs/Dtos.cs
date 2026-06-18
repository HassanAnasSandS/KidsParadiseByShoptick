namespace KidsParadiseByShoptick.Application.DTOs;

public record CategoryDto(int Id, string Name, string? ImageUrl, int ToyCount);
public record CategoryDetailDto(int Id, string Name, string? ImageUrl, IReadOnlyList<ToyListDto> Toys);

public record ToyListDto(
    int Id, string Name, decimal Price, decimal? SalePrice, bool IsSold,
    IReadOnlyList<string> ImageUrls, string CategoryName, double? AverageRating);

public record ToyDetailDto(
    int Id, string Name, decimal Price, decimal? SalePrice, bool IsSold,
    IReadOnlyList<string> ImageUrls, string CategoryName, int CategoryId,
    double? AverageRating, int ReviewCount);

public record ReviewDto(
    int Id, string ReviewerName, int Rating, string Comment, string? ImageUrl,
    string ToyName, int ToyId, string OrderNumber, DateTime CreatedAt);

public record PendingReviewDto(
    int OrderId, string OrderNumber, int ToyId, string ToyName, string? ToyImageUrl);

public record CreateReviewRequest(
    string Email, int OrderId, int ToyId, string ReviewerName, int Rating, string Comment, string? ImagePath);

public record AdminUpdateReviewRequest(string ReviewerName, int Rating, string Comment, string? ImagePath);

public record ReviewEligibilityDto(bool CanReview, string? Message);

public record PlaceOrderRequest(
    string Email, string Name, string Phone, string Whatsapp, string City, string Address,
    IReadOnlyList<int> ToyIds);

public record OrderItemDto(int ToyId, string ToyName, decimal Price, string? ImageUrl);

public record OrderDto(
    int Id, string OrderNumber, string Status, decimal SubTotal,
    decimal DeliveryCharge, decimal Total, decimal? AdvanceAmount, decimal BalanceAmount,
    string City, string Address,
    string Phone, string Whatsapp, string? TrackingNumber,
    string CustomerName, string CustomerEmail,
    DateTime CreatedAt, IReadOnlyList<OrderItemDto> Items);

public record OrderPlacedDto(string OrderNumber, decimal Total, decimal DeliveryCharge);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record AdminLoginRequest(string Username, string Password);
public record AdminLoginResponse(string Token, string Username);

public record CreateCategoryRequest(string Name, string? ImagePath);
public record UpdateCategoryRequest(string Name, string? ImagePath);

public record CreateToyRequest(
    int CategoryId, string Name, decimal Price, decimal? SalePrice,
    IReadOnlyList<string> ImagePaths);

public record UpdateToyRequest(
    int CategoryId, string Name, decimal Price, decimal? SalePrice,
    IReadOnlyList<string> ImagePaths);

public record UpdateOrderStatusRequest(string Status, string? TrackingNumber, decimal? AdvanceAmount);

public record UploadResponse(string Path, string Url);
