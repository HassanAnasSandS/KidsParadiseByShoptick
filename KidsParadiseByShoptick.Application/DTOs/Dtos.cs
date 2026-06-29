namespace KidsParadiseByShoptick.Application.DTOs;

public record CategoryDto(int Id, string Name, string? ImageUrl, string? ImagePath, int ToyCount);
public record CategoryDetailDto(int Id, string Name, string? ImageUrl, int AvailableToyCount);

public record ToyListDto(
    int Id, string Name, decimal Price, decimal? SalePrice, bool IsSold,
    IReadOnlyList<string> ImageUrls, string CategoryName, double? AverageRating);

public record ToyDetailDto(
    int Id, string Name, decimal Price, decimal? SalePrice, bool IsSold,
    IReadOnlyList<string> ImagePaths, IReadOnlyList<string> ImageUrls, string CategoryName, int CategoryId,
    double? AverageRating, int ReviewCount, string? VideoLink);

public record SocialPostResultDto(
    bool FacebookPosted,
    string? FacebookPostId,
    bool InstagramPosted,
    string? InstagramPostId,
    string? Message);

public record AdminToySaveResponse(ToyListDto Toy, SocialPostResultDto SocialPost);

public record ReviewDto(
    int Id, string ReviewerName, int Rating, string Comment, string? ImageUrl, string? ImagePath,
    string ToyName, int ToyId, string OrderNumber, DateTime CreatedAt);

public record PendingReviewDto(
    int OrderId, string OrderNumber, int ToyId, string ToyName, string? ToyImageUrl);

public record CreateReviewRequest(
    string Whatsapp, int OrderId, int ToyId, string ReviewerName, int Rating, string Comment, string? ImagePath);

public record AdminUpdateReviewRequest(string ReviewerName, int Rating, string Comment, string? ImagePath);

public record ReviewEligibilityDto(bool CanReview, string? Message);

public record PlaceOrderRequest(
    string Name, string Whatsapp, string City, string Address,
    IReadOnlyList<int> ToyIds);

public record OrderItemDto(int ToyId, string ToyName, decimal Price, string? ImageUrl);

public record OrderDto(
    int Id, string OrderNumber, string Status, decimal SubTotal,
    decimal DeliveryCharge, decimal Total, decimal? AdvanceAmount, decimal? DiscountAmount, decimal BalanceAmount,
    string City, string Address,
    string Whatsapp, string? TrackingNumber,
    string CustomerName,
    DateTime CreatedAt, IReadOnlyList<OrderItemDto> Items);

public record OrderPlacedDto(string OrderNumber, decimal Total, decimal DeliveryCharge);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record AdminLoginRequest(string Username, string Password, bool RememberMe = false);
public record AdminLoginResponse(string Token, string Username);

public record CreateCategoryRequest(string Name, string? ImagePath);
public record UpdateCategoryRequest(string Name, string? ImagePath);

public record CreateToyRequest(
    int CategoryId, string Name, decimal Price, decimal? SalePrice,
    IReadOnlyList<string> ImagePaths, string? VideoLink = null);

public record UpdateToyRequest(
    int CategoryId, string Name, decimal Price, decimal? SalePrice,
    IReadOnlyList<string> ImagePaths, string? VideoLink = null);

public record UpdateOrderStatusRequest(string Status, string? TrackingNumber, decimal? AdvanceAmount, decimal? DiscountAmount);

public record OrderStatusCountsDto(
    int Total,
    int Pending,
    int Confirmed,
    int Shipped,
    int Delivered,
    int Cancelled);

public record AdminUpdateOrderRequest(
    string CustomerName,
    string Whatsapp,
    string City,
    string Address,
    decimal DeliveryCharge,
    decimal? AdvanceAmount,
    decimal? DiscountAmount,
    string? TrackingNumber,
    IReadOnlyList<int> ToyIds);

public record UploadResponse(string Path, string Url);

public record AdminDashboardDto(
    int TotalToys,
    int TotalAvailableToys,
    int TotalSoldToys,
    int TotalToysOnSale,
    int TotalToysOnRegular,
    decimal RegularToysTotalAmount,
    decimal OnSaleToysTotalAmount,
    decimal AllToysTotalAmount,
    decimal AvailableToysTotalAmount,
    decimal AllSoldToysTotalAmount,
    int TotalCustomers,
    int TotalDeliveredOrders,
    decimal AllDeliveredOrdersTotalAmount);

public record SeoPublicConfigDto(
    string SiteName,
    string SiteBaseUrl,
    string DefaultTitle,
    string DefaultDescription,
    string DefaultKeywords,
    string DefaultOgImageUrl,
    string Locale,
    string Region);
