using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace KidsParadiseByShoptick.AdminApp.Models;

public class AdminLoginResponse
{
    [JsonPropertyName("token")] public string Token { get; set; } = string.Empty;
    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
}

public class OrderStatusCountsModel
{
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("pending")] public int Pending { get; set; }
    [JsonPropertyName("confirmed")] public int Confirmed { get; set; }
    [JsonPropertyName("shipped")] public int Shipped { get; set; }
    [JsonPropertyName("delivered")] public int Delivered { get; set; }
    [JsonPropertyName("cancelled")] public int Cancelled { get; set; }
}

public partial class StatusFilterOption : ObservableObject
{
    public string Value { get; init; } = "All";
    public string Label { get; init; } = "All";
    [ObservableProperty] private bool isSelected;
}

public class DashboardModel
{
    [JsonPropertyName("totalToys")] public int TotalToys { get; set; }
    [JsonPropertyName("totalAvailableToys")] public int TotalAvailableToys { get; set; }
    [JsonPropertyName("totalSoldToys")] public int TotalSoldToys { get; set; }
    [JsonPropertyName("totalToysOnSale")] public int TotalToysOnSale { get; set; }
    [JsonPropertyName("totalToysOnRegular")] public int TotalToysOnRegular { get; set; }
    [JsonPropertyName("regularToysTotalAmount")] public decimal RegularToysTotalAmount { get; set; }
    [JsonPropertyName("onSaleToysTotalAmount")] public decimal OnSaleToysTotalAmount { get; set; }
    [JsonPropertyName("allToysTotalAmount")] public decimal AllToysTotalAmount { get; set; }
    [JsonPropertyName("availableToysTotalAmount")] public decimal AvailableToysTotalAmount { get; set; }
    [JsonPropertyName("allSoldToysTotalAmount")] public decimal AllSoldToysTotalAmount { get; set; }
    [JsonPropertyName("totalCustomers")] public int TotalCustomers { get; set; }
    [JsonPropertyName("totalDeliveredOrders")] public int TotalDeliveredOrders { get; set; }
    [JsonPropertyName("allDeliveredOrdersTotalAmount")] public decimal AllDeliveredOrdersTotalAmount { get; set; }
    [JsonPropertyName("totalAffiliatePartners")] public int TotalAffiliatePartners { get; set; }
    [JsonPropertyName("activeAffiliatePartners")] public int ActiveAffiliatePartners { get; set; }
    [JsonPropertyName("affiliatedOrdersCount")] public int AffiliatedOrdersCount { get; set; }
    [JsonPropertyName("affiliateCommissionTotal")] public decimal AffiliateCommissionTotal { get; set; }
    [JsonPropertyName("affiliatePaidTotal")] public decimal AffiliatePaidTotal { get; set; }
    [JsonPropertyName("affiliateOutstandingTotal")] public decimal AffiliateOutstandingTotal { get; set; }
    [JsonPropertyName("affiliatePartners")] public List<DashboardAffiliatePartnerModel> AffiliatePartners { get; set; } = [];
}

public class DashboardAffiliatePartnerModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
    [JsonPropertyName("isActive")] public bool IsActive { get; set; }
    [JsonPropertyName("attributedOrders")] public int AttributedOrders { get; set; }
    [JsonPropertyName("totalCommission")] public decimal TotalCommission { get; set; }
    [JsonPropertyName("totalPaid")] public decimal TotalPaid { get; set; }
    [JsonPropertyName("balance")] public decimal Balance { get; set; }

    [JsonIgnore] public string StatusLabel => IsActive ? "Active" : "Inactive";
    [JsonIgnore] public string CommissionText => $"Commission: Rs. {TotalCommission:N0}";
    [JsonIgnore] public string PaidText => $"Paid: Rs. {TotalPaid:N0}";
    [JsonIgnore] public string BalanceText => $"Outstanding: Rs. {Balance:N0}";
    [JsonIgnore] public string OrdersText => $"Orders: {AttributedOrders}";
}

public class CategoryModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
    [JsonPropertyName("imagePath")] public string? ImagePath { get; set; }
    [JsonPropertyName("toyCount")] public int ToyCount { get; set; }
}

public class ToyListModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("price")] public decimal Price { get; set; }
    [JsonPropertyName("salePrice")] public decimal? SalePrice { get; set; }
    [JsonPropertyName("isSold")] public bool IsSold { get; set; }
    [JsonPropertyName("imageUrls")] public List<string> ImageUrls { get; set; } = [];
    [JsonPropertyName("categoryName")] public string CategoryName { get; set; } = string.Empty;
    [JsonPropertyName("averageRating")] public double? AverageRating { get; set; }

    public string PrimaryImage => ImageUrls?.FirstOrDefault() ?? string.Empty;
    public decimal EffectivePrice => SalePrice ?? Price;
}

public class ToyImageSearchMatchModel : ToyListModel
{
    [JsonPropertyName("score")] public double Score { get; set; }
    [JsonPropertyName("matchType")] public string MatchType { get; set; } = string.Empty;

    public string MatchLabel => MatchType switch
    {
        "exact" => "Exact image",
        "near-exact" => "Near-exact image",
        "similar" => $"Similar ({Score:P0})",
        _ => $"Match ({Score:P0})",
    };
}

public class ToyImageIndexResultModel
{
    [JsonPropertyName("indexed")] public int Indexed { get; set; }
    [JsonPropertyName("skipped")] public int Skipped { get; set; }
    [JsonPropertyName("failed")] public int Failed { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}

public class ToyDetailModel : ToyListModel
{
    [JsonPropertyName("categoryId")] public int CategoryId { get; set; }
    [JsonPropertyName("imagePaths")] public List<string> ImagePaths { get; set; } = [];
    [JsonPropertyName("reviewCount")] public int ReviewCount { get; set; }
    [JsonPropertyName("videoLink")] public string? VideoLink { get; set; }
    [JsonPropertyName("videoFilePath")] public string? VideoFilePath { get; set; }
    [JsonPropertyName("videoFileUrl")] public string? VideoFileUrl { get; set; }
}

public class ReviewModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("reviewerName")] public string ReviewerName { get; set; } = string.Empty;
    [JsonPropertyName("rating")] public int Rating { get; set; }
    [JsonPropertyName("comment")] public string Comment { get; set; } = string.Empty;
    [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
    [JsonPropertyName("imagePath")] public string? ImagePath { get; set; }
    [JsonPropertyName("toyName")] public string ToyName { get; set; } = string.Empty;
    [JsonPropertyName("toyId")] public int ToyId { get; set; }
    [JsonPropertyName("orderNumber")] public string OrderNumber { get; set; } = string.Empty;
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
}

public class OrderItemModel
{
    [JsonPropertyName("toyId")] public int ToyId { get; set; }
    [JsonPropertyName("toyName")] public string ToyName { get; set; } = string.Empty;
    [JsonPropertyName("price")] public decimal Price { get; set; }
    [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }

    [JsonIgnore] public decimal LineTotal => Price;
    [JsonIgnore] public string PriceLine => $"Rs. {Price:N0}";
}

public class OrderModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("orderNumber")] public string OrderNumber { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("subTotal")] public decimal SubTotal { get; set; }
    [JsonPropertyName("deliveryCharge")] public decimal DeliveryCharge { get; set; }
    [JsonPropertyName("total")] public decimal Total { get; set; }
    [JsonPropertyName("advanceAmount")] public decimal? AdvanceAmount { get; set; }
    [JsonPropertyName("discountAmount")] public decimal? DiscountAmount { get; set; }
    [JsonPropertyName("balanceAmount")] public decimal BalanceAmount { get; set; }
    [JsonPropertyName("city")] public string City { get; set; } = string.Empty;
    [JsonPropertyName("address")] public string Address { get; set; } = string.Empty;
    [JsonPropertyName("whatsapp")] public string Whatsapp { get; set; } = string.Empty;
    [JsonPropertyName("trackingNumber")] public string? TrackingNumber { get; set; }
    [JsonPropertyName("customerName")] public string CustomerName { get; set; } = string.Empty;
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("items")] public List<OrderItemModel> Items { get; set; } = [];
    [JsonPropertyName("affiliatePartnerId")] public int? AffiliatePartnerId { get; set; }
    [JsonPropertyName("affiliateCode")] public string? AffiliateCode { get; set; }
    [JsonPropertyName("affiliateName")] public string? AffiliateName { get; set; }
    [JsonPropertyName("affiliateCommissionAmount")] public decimal? AffiliateCommissionAmount { get; set; }
    [JsonPropertyName("affiliateCommissionStatus")] public string? AffiliateCommissionStatus { get; set; }

    [JsonIgnore] public bool HasAffiliate =>
        AffiliatePartnerId.HasValue
        || !string.IsNullOrWhiteSpace(AffiliateName)
        || !string.IsNullOrWhiteSpace(AffiliateCode);

    [JsonIgnore] public bool IsPending =>
        string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore] public string AffiliateDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(AffiliateName) && !string.IsNullOrWhiteSpace(AffiliateCode))
                return $"{AffiliateName} ({AffiliateCode})";
            if (!string.IsNullOrWhiteSpace(AffiliateName))
                return AffiliateName;
            if (!string.IsNullOrWhiteSpace(AffiliateCode))
                return AffiliateCode;
            return AffiliatePartnerId.HasValue ? $"Partner #{AffiliatePartnerId}" : string.Empty;
        }
    }

    [JsonIgnore] public string AffiliateListLabel =>
        HasAffiliate ? $"Affiliate: {AffiliateDisplay}" : string.Empty;

    [JsonIgnore] public string AffiliateCommissionDetail
    {
        get
        {
            if (!HasAffiliate || !AffiliateCommissionAmount.HasValue)
                return string.Empty;

            var amount = $"Rs. {AffiliateCommissionAmount.Value:N0}";
            return AffiliateCommissionStatus switch
            {
                "Earned" => $"Commission: {amount} (Earned)",
                "Reversed" => $"Commission: {amount} (Reversed)",
                "Pending" => $"Commission: {amount} (Pending — on delivery)",
                _ => $"Commission: {amount}",
            };
        }
    }
}

public class OrderPlacedModel
{
    [JsonPropertyName("orderNumber")] public string OrderNumber { get; set; } = string.Empty;
    [JsonPropertyName("total")] public decimal Total { get; set; }
    [JsonPropertyName("deliveryCharge")] public decimal DeliveryCharge { get; set; }
}

public class SiteImageModel
{
    [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
    [JsonPropertyName("group")] public string Group { get; set; } = string.Empty;
    [JsonPropertyName("sortOrder")] public int SortOrder { get; set; }
    [JsonPropertyName("imageUrl")] public string ImageUrl { get; set; } = string.Empty;
    [JsonPropertyName("defaultUrl")] public string DefaultUrl { get; set; } = string.Empty;
    [JsonPropertyName("isCustom")] public bool IsCustom { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("subtitle")] public string? Subtitle { get; set; }
    [JsonPropertyName("ctaText")] public string? CtaText { get; set; }
    [JsonPropertyName("linkUrl")] public string? LinkUrl { get; set; }
    [JsonPropertyName("titleColor")] public string? TitleColor { get; set; }
    [JsonPropertyName("subtitleColor")] public string? SubtitleColor { get; set; }
    [JsonPropertyName("ctaColor")] public string? CtaColor { get; set; }
    [JsonPropertyName("supportsText")] public bool SupportsText { get; set; }
}

public class UploadResult
{
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
}

public class SocialPostResultModel
{
    [JsonPropertyName("facebookPosted")] public bool FacebookPosted { get; set; }
    [JsonPropertyName("facebookPostId")] public string? FacebookPostId { get; set; }
    [JsonPropertyName("instagramPosted")] public bool InstagramPosted { get; set; }
    [JsonPropertyName("instagramPostId")] public string? InstagramPostId { get; set; }
    [JsonPropertyName("whatsAppCatalogPosted")] public bool WhatsAppCatalogPosted { get; set; }
    [JsonPropertyName("whatsAppCatalogProductId")] public string? WhatsAppCatalogProductId { get; set; }
    [JsonPropertyName("tikTokPosted")] public bool TikTokPosted { get; set; }
    [JsonPropertyName("tikTokPublishId")] public string? TikTokPublishId { get; set; }
    [JsonPropertyName("pinterestPosted")] public bool PinterestPosted { get; set; }
    [JsonPropertyName("pinterestPinId")] public string? PinterestPinId { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("queued")] public bool Queued { get; set; }
}

public class AdminToySaveResponseModel
{
    [JsonPropertyName("toy")] public ToyListModel Toy { get; set; } = new();
    [JsonPropertyName("socialPost")] public SocialPostResultModel SocialPost { get; set; } = new();
}

public class PagedResult<T>
{
    [JsonPropertyName("items")] public List<T> Items { get; set; } = [];
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("pageSize")] public int PageSize { get; set; }
}

public class SocialPostActionsModel
{
    [JsonPropertyName("facebookPhotos")] public bool FacebookPhotos { get; set; } = true;
    [JsonPropertyName("instagramPhotos")] public bool InstagramPhotos { get; set; } = true;
    [JsonPropertyName("whatsAppCatalog")] public bool WhatsAppCatalog { get; set; } = true;
    [JsonPropertyName("tikTokPhotos")] public bool TikTokPhotos { get; set; } = true;
    [JsonPropertyName("pinterest")] public bool Pinterest { get; set; } = true;
    [JsonPropertyName("youTube")] public bool YouTube { get; set; } = true;
    [JsonPropertyName("metaVideo")] public bool MetaVideo { get; set; } = true;
    [JsonPropertyName("tikTokVideo")] public bool TikTokVideo { get; set; } = true;

    public bool HasAnyServerAction =>
        FacebookPhotos || InstagramPhotos || WhatsAppCatalog || TikTokPhotos || Pinterest;

    public bool HasAnyVideoAction => YouTube || MetaVideo || TikTokVideo;
}

public class SocialMediaSettingsModel
{
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("tags")] public string Tags { get; set; } = string.Empty;
    [JsonPropertyName("tikTokPostMode")] public string TikTokPostMode { get; set; } = "DIRECT_POST";
    [JsonPropertyName("onCreate")] public SocialPostActionsModel OnCreate { get; set; } = new();
    [JsonPropertyName("onEdit")] public SocialPostActionsModel OnEdit { get; set; } = new();
}

public class DeliveryChargeSettingsModel
{
    [JsonPropertyName("karachi")] public decimal Karachi { get; set; }
    [JsonPropertyName("otherCities")] public decimal OtherCities { get; set; }
}

public class SiteSocialLinksModel
{
    [JsonPropertyName("whatsAppNumber")] public string WhatsAppNumber { get; set; } = string.Empty;
    [JsonPropertyName("whatsAppDisplay")] public string WhatsAppDisplay { get; set; } = string.Empty;
    [JsonPropertyName("youTubeUrl")] public string YouTubeUrl { get; set; } = string.Empty;
    [JsonPropertyName("facebookUrl")] public string FacebookUrl { get; set; } = string.Empty;
    [JsonPropertyName("instagramUrl")] public string InstagramUrl { get; set; } = string.Empty;
    [JsonPropertyName("tikTokUrl")] public string TikTokUrl { get; set; } = string.Empty;
    [JsonPropertyName("pinterestUrl")] public string PinterestUrl { get; set; } = string.Empty;
}

public class MetaRequirementCheckModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("step")] public int Step { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("isMet")] public bool IsMet { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("fixInstructions")] public string FixInstructions { get; set; } = string.Empty;
}

public class MetaRequirementsStatusModel
{
    [JsonPropertyName("allMet")] public bool AllMet { get; set; }
    [JsonPropertyName("facebookPageId")] public string? FacebookPageId { get; set; }
    [JsonPropertyName("whatsAppBusinessAccountId")] public string? WhatsAppBusinessAccountId { get; set; }
    [JsonPropertyName("whatsAppCatalogId")] public string? WhatsAppCatalogId { get; set; }
    [JsonPropertyName("requirements")] public List<MetaRequirementCheckModel> Requirements { get; set; } = [];
}

public class MetaUploadCredentialsModel
{
    [JsonPropertyName("facebookPageId")] public string FacebookPageId { get; set; } = string.Empty;
    [JsonPropertyName("pageAccessToken")] public string PageAccessToken { get; set; } = string.Empty;
    [JsonPropertyName("instagramBusinessAccountId")] public string? InstagramBusinessAccountId { get; set; }
    [JsonPropertyName("whatsAppNumber")] public string? WhatsAppNumber { get; set; }
}

public class MetaConnectionStatusModel
{
    [JsonPropertyName("connected")] public bool Connected { get; set; }
    [JsonPropertyName("facebookPageId")] public string? FacebookPageId { get; set; }
    [JsonPropertyName("whatsAppBusinessAccountId")] public string? WhatsAppBusinessAccountId { get; set; }
    [JsonPropertyName("whatsAppCatalogId")] public string? WhatsAppCatalogId { get; set; }
    [JsonPropertyName("allRequirementsMet")] public bool AllRequirementsMet { get; set; }
    [JsonPropertyName("metCount")] public int MetCount { get; set; }
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
}

public class YouTubeConnectionStatusModel
{
    [JsonPropertyName("configured")] public bool Configured { get; set; }
    [JsonPropertyName("connected")] public bool Connected { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}

public class YouTubeAuthUrlModel
{
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    [JsonPropertyName("message")] public string? Message { get; set; }
}

public class ApiError
{
    [JsonPropertyName("message")] public string? Message { get; set; }
}

public class AffiliatePartnerModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("whatsapp")] public string? Whatsapp { get; set; }
    [JsonPropertyName("accountNumber")] public string? AccountNumber { get; set; }
    [JsonPropertyName("walletBankName")] public string? WalletBankName { get; set; }
    [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
    [JsonPropertyName("isActive")] public bool IsActive { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("totalCommission")] public decimal TotalCommission { get; set; }
    [JsonPropertyName("totalPaid")] public decimal TotalPaid { get; set; }
    [JsonPropertyName("balance")] public decimal Balance { get; set; }
    [JsonPropertyName("attributedOrders")] public int AttributedOrders { get; set; }

    [JsonIgnore] public string StatusLabel => IsActive ? "Active" : "Inactive";
    [JsonIgnore] public string ReferralLink =>
        $"{KidsParadiseByShoptick.AdminApp.Config.AppSettings.SiteBaseUrl.TrimEnd('/')}/?aff={Uri.EscapeDataString(Code)}";
    [JsonIgnore] public string LedgerLink =>
        $"{KidsParadiseByShoptick.AdminApp.Config.AppSettings.SiteBaseUrl.TrimEnd('/')}/partner?code={Uri.EscapeDataString(Code)}";
    [JsonIgnore] public string BalanceText => $"Balance: Rs. {Balance:N0}";
    [JsonIgnore] public string SummaryText =>
        $"Orders: {AttributedOrders} · Commission: Rs. {TotalCommission:N0} · Paid: Rs. {TotalPaid:N0}";
    [JsonIgnore] public string ContactSummary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Whatsapp)) parts.Add($"WhatsApp: {Whatsapp}");
            if (!string.IsNullOrWhiteSpace(WalletBankName)) parts.Add(WalletBankName);
            if (!string.IsNullOrWhiteSpace(AccountNumber)) parts.Add($"A/C: {AccountNumber}");
            return parts.Count == 0 ? string.Empty : string.Join(" · ", parts);
        }
    }

    [JsonIgnore]
    public string PartnerWelcomeMessage
    {
        get
        {
            var site = KidsParadiseByShoptick.AdminApp.Config.AppSettings.SiteBaseUrl.TrimEnd('/');
            return
                $"Hi {Name},\n\n" +
                "Welcome to the Kids Paradise Affiliate Partner program!\n\n" +
                $"Your Affiliate Code: {Code}\n" +
                $"Your Affiliate Link: {site}/?aff={Code}\n\n" +
                "Check your commission ledger (read-only):\n" +
                $"{site}/partner?code={Code}\n\n" +
                "You earn a flat 10% commission on every order placed through your link " +
                "(after discount, excluding delivery charges).\n\n" +
                "Thank you!\n" +
                "Kids Paradise by Shoptick";
        }
    }
}

public class AffiliateLedgerEntryModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("orderId")] public int? OrderId { get; set; }
    [JsonPropertyName("orderNumber")] public string? OrderNumber { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }

    [JsonIgnore] public string AmountDisplay => Type switch
    {
        "Payment" or "Reversal" => $"- Rs. {Amount:N2}",
        _ => $"+ Rs. {Amount:N2}",
    };

    [JsonIgnore] public string AmountColor => Type switch
    {
        "Commission" => "#0f766e",
        "Payment" => "#1d4ed8",
        "Reversal" => "#b91c1c",
        _ => "#334155",
    };

    [JsonIgnore] public string TypeLabel => Type switch
    {
        "Commission" => "Commission (+)",
        "Payment" => "Payment (−)",
        "Reversal" => "Reversal (−)",
        _ => Type,
    };
}

public class AffiliateLedgerModel
{
    [JsonPropertyName("partner")] public AffiliatePartnerModel Partner { get; set; } = new();
    [JsonPropertyName("totalCommission")] public decimal TotalCommission { get; set; }
    [JsonPropertyName("totalPaid")] public decimal TotalPaid { get; set; }
    [JsonPropertyName("balance")] public decimal Balance { get; set; }
    [JsonPropertyName("entries")] public List<AffiliateLedgerEntryModel> Entries { get; set; } = [];
}

