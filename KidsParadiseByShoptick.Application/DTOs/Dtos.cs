namespace KidsParadiseByShoptick.Application.DTOs;

public record CategoryDto(int Id, string Name, string? ImageUrl, string? ImagePath, int ToyCount);
public record CategoryDetailDto(int Id, string Name, string? ImageUrl, int AvailableToyCount);

public record ToyListDto(
    int Id, string Name, decimal Price, decimal? SalePrice, bool IsSold,
    IReadOnlyList<string> ImageUrls, string CategoryName, double? AverageRating);

public record ToyDetailDto(
    int Id, string Name, decimal Price, decimal? SalePrice, bool IsSold,
    IReadOnlyList<string> ImagePaths, IReadOnlyList<string> ImageUrls, string CategoryName, int CategoryId,
    double? AverageRating, int ReviewCount, string? VideoLink,
    string? VideoFilePath = null, string? VideoFileUrl = null);

public record SocialPostResultDto(
    bool FacebookPosted,
    string? FacebookPostId,
    bool InstagramPosted,
    string? InstagramPostId,
    string? Message,
    bool Queued = false,
    bool WhatsAppCatalogPosted = false,
    string? WhatsAppCatalogProductId = null,
    bool TikTokPosted = false,
    string? TikTokPublishId = null,
    bool PinterestPosted = false,
    string? PinterestPinId = null);

public record SocialPostActionsDto(
    bool FacebookPhotos = true,
    bool InstagramPhotos = true,
    bool WhatsAppCatalog = true,
    bool TikTokPhotos = true,
    bool Pinterest = true,
    bool YouTube = true,
    bool MetaVideo = true,
    bool TikTokVideo = true)
{
    public static SocialPostActionsDto AllEnabled { get; } = new();

    public bool HasAnyServerAction =>
        FacebookPhotos || InstagramPhotos || WhatsAppCatalog || TikTokPhotos || Pinterest;

    public bool HasAnyVideoAction => YouTube || MetaVideo || TikTokVideo;
}

public record SocialMediaSettingsDto(
    string Description,
    string Tags,
    string TikTokPostMode = "DIRECT_POST",
    SocialPostActionsDto? OnCreate = null,
    SocialPostActionsDto? OnEdit = null);

public record UpdateSocialMediaSettingsRequest(
    string Description,
    string Tags,
    string? TikTokPostMode = null,
    SocialPostActionsDto? OnCreate = null,
    SocialPostActionsDto? OnEdit = null);

public record UpdateTikTokPostModeRequest(string PostMode);

public enum SocialPostTrigger
{
    Create = 0,
    Edit = 1,
}

public record DeliveryChargeSettingsDto(decimal Karachi, decimal OtherCities);

public record UpdateDeliveryChargeSettingsRequest(decimal Karachi, decimal OtherCities);

public record SiteSocialLinksDto(
    string WhatsAppNumber,
    string WhatsAppDisplay,
    string YouTubeUrl,
    string FacebookUrl,
    string InstagramUrl,
    string TikTokUrl,
    string PinterestUrl);

public record UpdateSiteSocialLinksRequest(
    string WhatsAppNumber,
    string WhatsAppDisplay,
    string? YouTubeUrl,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TikTokUrl,
    string? PinterestUrl);

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
    IReadOnlyList<int> ToyIds,
    string? AffiliateCode);

public record OrderItemDto(int ToyId, string ToyName, decimal Price, string? ImageUrl);

public record OrderDto(
    int Id, string OrderNumber, string Status, decimal SubTotal,
    decimal DeliveryCharge, decimal Total, decimal? AdvanceAmount, decimal? DiscountAmount, decimal BalanceAmount,
    string City, string Address,
    string Whatsapp, string? TrackingNumber,
    string CustomerName,
    DateTime CreatedAt, IReadOnlyList<OrderItemDto> Items,
    int? AffiliatePartnerId,
    string? AffiliateCode,
    string? AffiliateName,
    decimal? AffiliateCommissionAmount,
    string? AffiliateCommissionStatus);

public record OrderPlacedDto(string OrderNumber, decimal Total, decimal DeliveryCharge);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record AdminLoginRequest(string Username, string Password, bool RememberMe = false);
public record AdminLoginResponse(string Token, string Username);

public record CreateCategoryRequest(string Name, string? ImagePath);
public record UpdateCategoryRequest(string Name, string? ImagePath);

public record CreateToyRequest(
    int CategoryId, string Name, decimal Price, decimal? SalePrice,
    IReadOnlyList<string> ImagePaths, string? VideoLink = null,
    string? VideoFilePath = null);

public record UpdateToyRequest(
    int CategoryId, string Name, decimal Price, decimal? SalePrice,
    IReadOnlyList<string> ImagePaths, string? VideoLink = null,
    bool PostToSocialMedia = false,
    string? VideoFilePath = null);

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
    decimal AllDeliveredOrdersTotalAmount,
    int TotalAffiliatePartners,
    int ActiveAffiliatePartners,
    int AffiliatedOrdersCount,
    decimal AffiliateCommissionTotal,
    decimal AffiliatePaidTotal,
    decimal AffiliateOutstandingTotal,
    IReadOnlyList<DashboardAffiliatePartnerDto> AffiliatePartners);

public record DashboardAffiliatePartnerDto(
    int Id,
    string Name,
    string Code,
    bool IsActive,
    int AttributedOrders,
    decimal TotalCommission,
    decimal TotalPaid,
    decimal Balance);

public record SeoPublicConfigDto(
    string SiteName,
    string SiteBaseUrl,
    string DefaultTitle,
    string DefaultDescription,
    string DefaultKeywords,
    string DefaultOgImageUrl,
    string Locale,
    string Region);

public record ToyImageSearchMatchDto(
    int Id,
    string Name,
    decimal Price,
    decimal? SalePrice,
    bool IsSold,
    IReadOnlyList<string> ImageUrls,
    string CategoryName,
    double Score,
    string MatchType);

public record ToyImageIndexResultDto(
    int Indexed,
    int Skipped,
    int Failed,
    string Message);

public record AffiliatePartnerDto(
    int Id,
    string Name,
    string? Whatsapp,
    string? AccountNumber,
    string? WalletBankName,
    string Code,
    bool IsActive,
    DateTime CreatedAt,
    decimal TotalCommission,
    decimal TotalPaid,
    decimal Balance,
    int AttributedOrders);

public record CreateAffiliatePartnerRequest(
    string Name,
    string? Whatsapp,
    string? AccountNumber,
    string? WalletBankName,
    bool IsActive = true);

public record UpdateAffiliatePartnerRequest(
    string Name,
    string? Whatsapp,
    string? AccountNumber,
    string? WalletBankName,
    bool IsActive);

public record AffiliateLedgerEntryDto(
    int Id,
    string Type,
    decimal Amount,
    string? Description,
    int? OrderId,
    string? OrderNumber,
    DateTime CreatedAt);

public record AffiliateLedgerDto(
    AffiliatePartnerDto Partner,
    decimal TotalCommission,
    decimal TotalPaid,
    decimal Balance,
    IReadOnlyList<AffiliateLedgerEntryDto> Entries);

public record RecordAffiliatePaymentRequest(decimal Amount, string? Notes);

