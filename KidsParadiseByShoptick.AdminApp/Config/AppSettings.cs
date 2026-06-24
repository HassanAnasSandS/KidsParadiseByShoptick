namespace KidsParadiseByShoptick.AdminApp.Config;

public static class AppSettings
{
    public const string ApiBaseUrl = "https://kidsparadise.shoptick.shop/api";
    public const string SiteBaseUrl = "https://kidsparadise.shoptick.shop";

    public static string OrderAlertsHubUrl =>
        ApiBaseUrl.Replace("/api", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/') + "/hubs/admin-orders";

    public static string ResolveImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;
        return SiteBaseUrl.TrimEnd('/') + (url.StartsWith('/') ? url : "/" + url);
    }
}
