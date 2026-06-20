using System.Globalization;
using System.Text;
using System.Xml;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

public class SitemapService : ISitemapService
{
    private const string SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private readonly ISitemapRepository _sitemapRepository;

    public SitemapService(ISitemapRepository sitemapRepository) => _sitemapRepository = sitemapRepository;

    public async Task<string> GenerateSitemapXmlAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var root = NormalizeBaseUrl(baseUrl);
        var urls = new List<SitemapUrl>();

        urls.Add(new SitemapUrl($"{root}/", DateTime.UtcNow, "daily", "1.0"));
        urls.Add(new SitemapUrl($"{root}/shop", DateTime.UtcNow, "daily", "0.9"));
        urls.Add(new SitemapUrl($"{root}/reviews", DateTime.UtcNow, "weekly", "0.6"));
        urls.Add(new SitemapUrl($"{root}/track-order", DateTime.UtcNow, "monthly", "0.5"));

        foreach (var page in StaticPages)
            urls.Add(new SitemapUrl($"{root}{page.Path}", DateTime.UtcNow, "monthly", "0.5"));

        var categories = await _sitemapRepository.GetCategoriesAsync(cancellationToken);
        foreach (var category in categories)
            urls.Add(new SitemapUrl($"{root}/category/{category.Id}", category.LastModified, "weekly", "0.8"));

        var products = await _sitemapRepository.GetAvailableProductsAsync(cancellationToken);
        foreach (var product in products)
            urls.Add(new SitemapUrl($"{root}/product/{product.Id}", product.LastModified, "daily", "0.6"));

        return BuildXml(urls);
    }

    public string GenerateRobotsTxt(string baseUrl)
    {
        var root = NormalizeBaseUrl(baseUrl);
        return $"""
            User-agent: *
            Allow: /
            Disallow: /admin/
            Disallow: /api/admin/
            Disallow: /checkout
            Disallow: /cart
            Disallow: /order-success/

            Sitemap: {root}/sitemap.xml
            """;
    }

    private static string BuildXml(IReadOnlyList<SitemapUrl> urls)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            Async = false,
        };

        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw, settings);

        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", SitemapNs);

        foreach (var url in urls)
        {
            writer.WriteStartElement("url", SitemapNs);
            writer.WriteElementString("loc", SitemapNs, url.Loc);
            writer.WriteElementString("lastmod", SitemapNs, url.LastMod.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            writer.WriteElementString("changefreq", SitemapNs, url.ChangeFreq);
            writer.WriteElementString("priority", SitemapNs, url.Priority);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();

        return sw.ToString();
    }

    private static string NormalizeBaseUrl(string baseUrl)
        => baseUrl.TrimEnd('/');

    private static readonly (string Path, string Title)[] StaticPages =
    [
        ("/about", "About"),
        ("/contact", "Contact"),
        ("/privacy-policy", "Privacy Policy"),
    ];

    private sealed record SitemapUrl(string Loc, DateTime LastMod, string ChangeFreq, string Priority);
}
