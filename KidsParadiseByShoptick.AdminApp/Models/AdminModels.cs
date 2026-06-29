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

public class ToyDetailModel : ToyListModel
{
    [JsonPropertyName("categoryId")] public int CategoryId { get; set; }
    [JsonPropertyName("imagePaths")] public List<string> ImagePaths { get; set; } = [];
    [JsonPropertyName("reviewCount")] public int ReviewCount { get; set; }
    [JsonPropertyName("videoLink")] public string? VideoLink { get; set; }
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
    [JsonPropertyName("message")] public string? Message { get; set; }
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

public class ApiError
{
    [JsonPropertyName("message")] public string? Message { get; set; }
}
