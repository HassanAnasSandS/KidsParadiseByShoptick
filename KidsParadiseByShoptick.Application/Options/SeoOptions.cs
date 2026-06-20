namespace KidsParadiseByShoptick.Application.Options;

public class SeoOptions
{
    public const string SectionName = "Seo";

    /// <summary>Canonical site URL without trailing slash.</summary>
    public string SiteBaseUrl { get; set; } = "https://www.kidsparadisethetoyshop.store";

    /// <summary>Canonical host only, e.g. www.kidsparadisethetoyshop.store</summary>
    public string CanonicalHost { get; set; } = "www.kidsparadisethetoyshop.store";

    /// <summary>When true, HTTP→HTTPS and non-www→www 301 redirects are enforced.</summary>
    public bool EnforceCanonicalHost { get; set; } = true;

    public int SitemapCacheMinutes { get; set; } = 60;
}
