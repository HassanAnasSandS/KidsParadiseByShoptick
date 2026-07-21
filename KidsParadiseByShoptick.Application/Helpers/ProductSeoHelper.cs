using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Options;

namespace KidsParadiseByShoptick.Application.Helpers;

/// <summary>Shared product SEO copy for React + server-injected crawler HTML.</summary>
public static class ProductSeoHelper
{
    public static string BuildTitle(string name)
        => Clip($"{name.Trim()} – Buy Online Pakistan", 58);

    public static string BuildDescription(
        string name, string categoryName, decimal price, decimal? salePrice, bool isSold = false)
    {
        var effective = salePrice is > 0 && salePrice < price ? salePrice.Value : price;
        var priceLabel = string.Create(CultureInfo.InvariantCulture, $"Rs. {effective:N0}");
        var sold = isSold ? " Currently sold out." : "";
        return Clip(
            $"Buy original {name.Trim()} in Pakistan with cash on delivery.{sold} {categoryName.Trim()} from Kids Paradise by Shoptick (Karachi). Price {priceLabel}. Fast delivery nationwide.",
            158);
    }

    public static string BuildKeywords(string name, string categoryName)
        => string.Join(", ",
            name.Trim(),
            $"buy {name.Trim()} Pakistan",
            categoryName.Trim(),
            "kids toys Pakistan",
            "toys Karachi cash on delivery",
            "online toy shop Pakistan",
            "Kids Paradise by Shoptick");

    public static string InjectIntoIndexHtml(
        string html,
        ToyDetailDto toy,
        SeoOptions seo,
        string absoluteImageUrl)
    {
        var root = seo.SiteBaseUrl.TrimEnd('/');
        var title = BuildTitle(toy.Name);
        var fullTitle = $"{title} | {seo.SiteName}";
        var description = BuildDescription(toy.Name, toy.CategoryName, toy.Price, toy.SalePrice, toy.IsSold);
        var keywords = BuildKeywords(toy.Name, toy.CategoryName);
        var canonical = $"{root}/product/{toy.Id}";
        var image = string.IsNullOrWhiteSpace(absoluteImageUrl) ? seo.DefaultOgImageUrl : absoluteImageUrl;
        var priceLabel = string.Create(CultureInfo.InvariantCulture, $"Rs. {(toy.SalePrice ?? toy.Price):N0}");

        var safeTitle = WebUtility.HtmlEncode(fullTitle);
        var safeDesc = WebUtility.HtmlEncode(description);
        var safeKeywords = WebUtility.HtmlEncode(keywords);
        var safeCanonical = WebUtility.HtmlEncode(canonical);
        var safeImage = WebUtility.HtmlEncode(image);
        var safeName = WebUtility.HtmlEncode(toy.Name);
        var jsonLd = BuildProductJsonLd(toy, root, description, image);

        html = Regex.Replace(html, @"<title>[\s\S]*?</title>", $"<title>{safeTitle}</title>", RegexOptions.IgnoreCase);
        html = Regex.Replace(
            html,
            @"<meta\s+name=[""']description[""'][^>]*>",
            $"<meta name=\"description\" content=\"{safeDesc}\" />",
            RegexOptions.IgnoreCase);
        html = Regex.Replace(
            html,
            @"<meta\s+name=[""']keywords[""'][^>]*>",
            $"<meta name=\"keywords\" content=\"{safeKeywords}\" />",
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
        html = Regex.Replace(
            html,
            @"<meta\s+property=[""']og:image[""'][^>]*>",
            $"<meta property=\"og:image\" content=\"{safeImage}\" />",
            RegexOptions.IgnoreCase);
        html = Regex.Replace(
            html,
            @"<meta\s+property=[""']og:type[""'][^>]*>",
            "<meta property=\"og:type\" content=\"product\" />",
            RegexOptions.IgnoreCase);

        var inject = new StringBuilder();
        inject.AppendLine($"    <meta name=\"product-seo\" content=\"kp-{toy.Id}\" />");
        inject.AppendLine($"    <script type=\"application/ld+json\">{jsonLd}</script>");
        inject.AppendLine(
            $"    <noscript><article><h1>{safeName}</h1><p>{safeDesc}</p>" +
            $"<p>Price: {WebUtility.HtmlEncode(priceLabel)}. Buy kids toys online in Pakistan with cash on delivery from Kids Paradise by Shoptick, Karachi.</p>" +
            $"<p><a href=\"{safeCanonical}\">View product</a></p></article></noscript>");

        return html.Replace("</head>", inject + "  </head>", StringComparison.OrdinalIgnoreCase);
    }

    static string BuildProductJsonLd(ToyDetailDto toy, string root, string description, string image)
    {
        var price = toy.SalePrice ?? toy.Price;
        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = toy.Name,
            ["description"] = description,
            ["image"] = toy.ImageUrls.Count > 0
                ? toy.ImageUrls.Select(u => Absolute(root, u)).ToArray()
                : new[] { image },
            ["sku"] = $"KP-{toy.Id}",
            ["brand"] = new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = "Kids Paradise by Shoptick" },
            ["category"] = toy.CategoryName,
            ["offers"] = new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["url"] = $"{root}/product/{toy.Id}",
                ["priceCurrency"] = "PKR",
                ["price"] = price.ToString(CultureInfo.InvariantCulture),
                ["availability"] = toy.IsSold
                    ? "https://schema.org/OutOfStock"
                    : "https://schema.org/InStock",
                ["itemCondition"] = "https://schema.org/UsedCondition",
                ["seller"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Organization",
                    ["name"] = "Kids Paradise by Shoptick",
                },
            },
        };

        return JsonSerializer.Serialize(payload);
    }

    static string Absolute(string root, string url)
    {
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;
        return root + (url.StartsWith('/') ? url : "/" + url);
    }

    static string Clip(string text, int max)
    {
        var clean = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (clean.Length <= max) return clean;
        var sliced = clean[..(max - 1)];
        var lastSpace = sliced.LastIndexOf(' ');
        return (lastSpace > 40 ? sliced[..lastSpace] : sliced).TrimEnd() + "…";
    }
}
