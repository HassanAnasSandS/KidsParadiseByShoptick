namespace KidsParadiseByShoptick.Application;

public record SiteImageDefinition(
    string Key,
    string Label,
    string Group,
    string DefaultUrl,
    int SortOrder,
    string? DefaultTitle = null,
    string? DefaultSubtitle = null,
    string? DefaultCtaText = null,
    string? DefaultLinkUrl = null,
    string? DefaultTitleColor = null,
    string? DefaultSubtitleColor = null,
    string? DefaultCtaColor = null,
    bool SupportsText = false);

public static class SiteImageDefaults
{
    public const string DefaultOverlayTextColor = "#FFFFFF";

    public static readonly IReadOnlyList<SiteImageDefinition> All =
    [
        new("favicon", "Favicon / Logo", "Brand", "/favicon.png", 0),
        new("hero_slide_1", "Hero Slide 1", "Hero Slider", "/hero/slide-1.jpg", 1,
            "Where Every Child's Dream Comes True",
            "Unique toys — one of a kind. Grab yours before it's gone!",
            "Shop Now", "/shop",
            DefaultOverlayTextColor, DefaultOverlayTextColor, DefaultOverlayTextColor,
            SupportsText: true),
        new("hero_slide_2", "Hero Slide 2", "Hero Slider", "/hero/slide-2.jpg", 2,
            "Joy in Every Box",
            "Quality toys delivered across Pakistan with love.",
            "Explore Toys", "/shop",
            DefaultOverlayTextColor, DefaultOverlayTextColor, DefaultOverlayTextColor,
            SupportsText: true),
        new("hero_slide_3", "Hero Slide 3", "Hero Slider", "/hero/slide-3.jpg", 3,
            "Soft Hugs & Big Smiles",
            "From plush friends to learning fun — find the perfect gift.",
            "Browse Collection", "/shop",
            DefaultOverlayTextColor, DefaultOverlayTextColor, DefaultOverlayTextColor,
            SupportsText: true),
        new("hero_slide_4", "Hero Slide 4", "Hero Slider", "/hero/slide-4.jpg", 4,
            "Easy Ordering",
            "{delivery} — 10% advance payment required.",
            "Order Today", "/shop",
            DefaultOverlayTextColor, DefaultOverlayTextColor, DefaultOverlayTextColor,
            SupportsText: true),
        new("banner_new_arrivals", "New Arrivals Banner", "Home Banners", "/hero/slide-1.jpg", 5,
            "New Arrivals",
            "Fresh toys added regularly",
            "Shop now →", "/shop",
            DefaultOverlayTextColor, DefaultOverlayTextColor, DefaultOverlayTextColor,
            SupportsText: true),
        new("banner_perfect_gifts", "Perfect Gifts Banner", "Home Banners", "/hero/slide-3.jpg", 6,
            "Perfect Gifts",
            "Make every birthday special",
            "Find gifts →", "/shop",
            DefaultOverlayTextColor, DefaultOverlayTextColor, DefaultOverlayTextColor,
            SupportsText: true),
        new("shop_header", "Shop Page Header", "Pages", "/hero/slide-1.jpg", 7,
            "Shop Kids Toys Online Pakistan",
            "{count} unique toys · cash on delivery · Karachi & nationwide",
            null, "/shop",
            DefaultOverlayTextColor, "#FFFFFFD9", DefaultOverlayTextColor,
            SupportsText: true),
    ];

    public static string GetDefaultUrl(string key)
        => All.FirstOrDefault(x => x.Key == key)?.DefaultUrl ?? string.Empty;

    public static SiteImageDefinition? GetDefinition(string key)
        => All.FirstOrDefault(x => x.Key == key);

    /// <summary>Normalizes #RGB / #RRGGBB / #RRGGBBAA (optional leading #). Returns null if invalid.</summary>
    public static string? NormalizeHexColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var hex = value.Trim();
        if (hex.StartsWith('#'))
            hex = hex[1..];

        if (hex.Length is not (3 or 6 or 8))
            return null;

        foreach (var c in hex)
        {
            var isHex = (c >= '0' && c <= '9')
                || (c >= 'a' && c <= 'f')
                || (c >= 'A' && c <= 'F');
            if (!isHex)
                return null;
        }

        return "#" + hex.ToUpperInvariant();
    }
}