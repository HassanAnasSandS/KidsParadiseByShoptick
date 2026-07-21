using System.Globalization;
using System.Text;
using System.Xml;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

public class SitemapService : ISitemapService
{
    private const string SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private const string ImageNs = "http://www.google.com/schemas/sitemap-image/1.1";

    private readonly ISitemapRepository _sitemapRepository;
    private readonly IFileStorageService _fileStorage;

    public SitemapService(ISitemapRepository sitemapRepository, IFileStorageService fileStorage)
    {
        _sitemapRepository = sitemapRepository;
        _fileStorage = fileStorage;
    }

    public async Task<string> GenerateSitemapXmlAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var root = NormalizeBaseUrl(baseUrl);
        var urls = new List<SitemapUrl>();

        urls.Add(new SitemapUrl($"{root}/", DateTime.UtcNow, "daily", "1.0", null, null));
        urls.Add(new SitemapUrl($"{root}/shop", DateTime.UtcNow, "daily", "0.9", null, null));
        urls.Add(new SitemapUrl($"{root}/reviews", DateTime.UtcNow, "weekly", "0.6", null, null));
        urls.Add(new SitemapUrl($"{root}/track-order", DateTime.UtcNow, "monthly", "0.5", null, null));

        foreach (var page in StaticPages)
            urls.Add(new SitemapUrl($"{root}{page.Path}", DateTime.UtcNow, "monthly", "0.5", null, null));

        var categories = await _sitemapRepository.GetCategoriesAsync(cancellationToken);
        foreach (var category in categories)
            urls.Add(new SitemapUrl($"{root}/category/{category.Id}", category.LastModified, "weekly", "0.8", null, null));

        var products = await _sitemapRepository.GetAvailableProductsWithImagesAsync(cancellationToken);
        foreach (var product in products)
        {
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(product.PrimaryImagePath))
            {
                var relative = _fileStorage.GetPublicUrl(product.PrimaryImagePath);
                imageUrl = relative.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? relative
                    : root + (relative.StartsWith('/') ? relative : "/" + relative);
            }

            urls.Add(new SitemapUrl(
                $"{root}/product/{product.Id}",
                product.LastModified,
                "daily",
                "0.8",
                imageUrl,
                product.Name));
        }

        return BuildXml(urls);
    }

    public string GenerateRobotsTxt(string baseUrl)
    {
        var root = NormalizeBaseUrl(baseUrl);
        return $"""
            # Kids Paradise by Shoptick — {root}
            User-agent: *
            Allow: /
            Disallow: /admin
            Disallow: /admin/
            Disallow: /api/admin/
            Disallow: /cart
            Disallow: /checkout
            Disallow: /order-success/

            User-agent: Googlebot
            Allow: /

            User-agent: Bingbot
            Allow: /

            Sitemap: {root}/sitemap.xml
            """;
    }

    private static string BuildXml(IReadOnlyList<SitemapUrl> urls)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),
            Async = false,
        };

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", SitemapNs);
            writer.WriteAttributeString("xmlns", "image", null, ImageNs);

            foreach (var url in urls)
            {
                writer.WriteStartElement("url", SitemapNs);
                writer.WriteElementString("loc", SitemapNs, url.Loc);
                writer.WriteElementString("lastmod", SitemapNs, url.LastMod.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", SitemapNs, url.ChangeFreq);
                writer.WriteElementString("priority", SitemapNs, url.Priority);

                if (!string.IsNullOrWhiteSpace(url.ImageUrl))
                {
                    writer.WriteStartElement("image", "image", ImageNs);
                    writer.WriteElementString("image", "loc", ImageNs, url.ImageUrl);
                    if (!string.IsNullOrWhiteSpace(url.ImageTitle))
                        writer.WriteElementString("image", "title", ImageNs, url.ImageTitle);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static string NormalizeBaseUrl(string baseUrl)
        => baseUrl.TrimEnd('/');

    private static readonly (string Path, string Title)[] StaticPages =
    [
        ("/about", "About"),
        ("/contact", "Contact"),
        ("/privacy-policy", "Privacy Policy"),
        ("/terms-of-service", "Terms of Service"),
    ];

    private sealed record SitemapUrl(
        string Loc,
        DateTime LastMod,
        string ChangeFreq,
        string Priority,
        string? ImageUrl,
        string? ImageTitle);
}
