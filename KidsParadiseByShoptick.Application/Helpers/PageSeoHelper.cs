using System.Net;
using System.Text.RegularExpressions;
using KidsParadiseByShoptick.Application.Options;

namespace KidsParadiseByShoptick.Application.Helpers;

/// <summary>
/// Server-side SEO injection for non-product SPA routes so crawlers see the
/// correct title/description/canonical instead of the home-page defaults.
/// </summary>
public static class PageSeoHelper
{
    public sealed record PageSeo(string Title, string Description, string Path);

    /// <summary>Static shop routes and their crawler meta (keep in sync with React PAGE_SEO).</summary>
    public static readonly IReadOnlyDictionary<string, PageSeo> StaticPages =
        new Dictionary<string, PageSeo>(StringComparer.OrdinalIgnoreCase)
        {
            ["/shop"] = new(
                "Shop Kids Toys Online Pakistan",
                "Browse kids toys online in Pakistan with cash on delivery. Soft toys, dolls, RC cars & educational toys from Kids Paradise by Shoptick — Karachi & nationwide delivery.",
                "/shop"),
            ["/reviews"] = new(
                "Customer Reviews",
                "Read verified customer reviews for toys purchased from Kids Paradise by Shoptick. Real feedback from parents across Pakistan.",
                "/reviews"),
            ["/about"] = new(
                "About Us",
                "Kids Paradise by Shoptick — online toys shop in Karachi & Pakistan. Unique kids toys, soft toys, educational toys with cash on delivery nationwide.",
                "/about"),
            ["/contact"] = new(
                "Contact Us",
                "Contact Kids Paradise by Shoptick via WhatsApp. Order help, delivery queries & toy inquiries for Karachi & all Pakistan.",
                "/contact"),
            ["/track-order"] = new(
                "Track Your Order",
                "Track your Kids Paradise by Shoptick order status using your WhatsApp number. See pending, confirmed, shipped & delivered updates.",
                "/track-order"),
            ["/privacy-policy"] = new(
                "Privacy Policy",
                "Privacy policy for Kids Paradise by Shoptick online toy shop at kidsparadise.shoptick.shop.",
                "/privacy-policy"),
            ["/terms-of-service"] = new(
                "Terms of Service",
                "Terms of service for Kids Paradise by Shoptick online toy shop at kidsparadise.shoptick.shop.",
                "/terms-of-service"),
        };

    public static PageSeo ForCategory(int id, string categoryName)
        => new(
            $"{categoryName.Trim()} — Kids Toys Online Pakistan",
            $"Buy {categoryName.Trim().ToLowerInvariant()} online in Pakistan with cash on delivery. Original kids toys from Kids Paradise by Shoptick (Karachi), fast nationwide delivery.",
            $"/category/{id}");

    public static string InjectIntoIndexHtml(string html, PageSeo page, SeoOptions seo)
    {
        var root = seo.SiteBaseUrl.TrimEnd('/');
        var fullTitle = $"{page.Title} | {seo.SiteName}";
        var canonical = $"{root}{page.Path}";

        var safeTitle = WebUtility.HtmlEncode(fullTitle);
        var safeDesc = WebUtility.HtmlEncode(page.Description);
        var safeCanonical = WebUtility.HtmlEncode(canonical);

        html = Regex.Replace(html, @"<title>[\s\S]*?</title>", $"<title>{safeTitle}</title>", RegexOptions.IgnoreCase);
        html = Regex.Replace(
            html,
            @"<meta\s+name=[""']description[""'][^>]*>",
            $"<meta name=\"description\" content=\"{safeDesc}\" />",
            RegexOptions.IgnoreCase);
        html = Regex.Replace(
            html,
            @"<link\s+rel=[""']canonical[""'][^>]*>",
            $"<link rel=\"canonical\" href=\"{safeCanonical}\" />",
            RegexOptions.IgnoreCase);
        html = Regex.Replace(
            html,
            @"<meta\s+property=[""']og:title[""'][^>]*>",
            $"<meta property=\"og:title\" content=\"{safeTitle}\" />",
            RegexOptions.IgnoreCase);
        html = Regex.Replace(
            html,
            @"<meta\s+property=[""']og:description[""'][^>]*>",
            $"<meta property=\"og:description\" content=\"{safeDesc}\" />",
            RegexOptions.IgnoreCase);
        html = Regex.Replace(
            html,
            @"<meta\s+property=[""']og:url[""'][^>]*>",
            $"<meta property=\"og:url\" content=\"{safeCanonical}\" />",
            RegexOptions.IgnoreCase);

        return html;
    }
}
