namespace KidsParadiseByShoptick.Application.Options;

public class SeoOptions
{
    public const string SectionName = "Seo";

    public string SiteName { get; set; } = "Kids Paradise by Shoptick";

    /// <summary>Canonical site URL without trailing slash.</summary>
    public string SiteBaseUrl { get; set; } = "https://kidsparadise.shoptick.shop";

    /// <summary>Canonical host only, e.g. kidsparadise.shoptick.shop</summary>
    public string CanonicalHost { get; set; } = "kidsparadise.shoptick.shop";

    public string DefaultTitle { get; set; } = "Kids Paradise by Shoptick — Online Toy Shop Karachi & Pakistan";

    public string DefaultDescription { get; set; } =
        "Shop unique kids toys online in Karachi & Pakistan. Soft toys, dolls, educational toys, cars & gifts with fast delivery. 10% advance, balance on delivery.";

    public string DefaultKeywords { get; set; } =
        "kids toys Pakistan, online toy shop Karachi, buy toys online Pakistan, toy store Pakistan, soft toys, educational toys, baby toys, Shoptick";

    /// <summary>Path under site root, e.g. /hero/slide-2.jpg</summary>
    public string DefaultOgImagePath { get; set; } = "/hero/slide-2.jpg";

    public string Locale { get; set; } = "en_PK";

    public string Region { get; set; } = "PK";

    /// <summary>When true, HTTP→HTTPS and non-canonical host 301 redirects are enforced.</summary>
    public bool EnforceCanonicalHost { get; set; } = true;

    public int SitemapCacheMinutes { get; set; } = 60;

    public string DefaultOgImageUrl => $"{SiteBaseUrl.TrimEnd('/')}{DefaultOgImagePath}";
}
