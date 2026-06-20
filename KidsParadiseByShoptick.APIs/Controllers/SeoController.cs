using System.Text;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.APIs.Controllers;

[ApiController]
public class SeoController : ControllerBase
{
    private readonly ISitemapService _sitemapService;
    private readonly SeoOptions _seoOptions;
    private readonly IMemoryCache _cache;

    public SeoController(ISitemapService sitemapService, IOptions<SeoOptions> seoOptions, IMemoryCache cache)
    {
        _sitemapService = sitemapService;
        _seoOptions = seoOptions.Value;
        _cache = cache;
    }

    [HttpGet("/sitemap.xml")]
    [Produces("application/xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var baseUrl = ResolveBaseUrl();
        var cacheKey = $"sitemap-xml:{baseUrl}";

        if (!_cache.TryGetValue(cacheKey, out string? xml))
        {
            xml = await _sitemapService.GenerateSitemapXmlAsync(baseUrl, cancellationToken);
            _cache.Set(cacheKey, xml, TimeSpan.FromMinutes(_seoOptions.SitemapCacheMinutes));
        }

        return Content(xml!, "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    [Produces("text/plain")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        var baseUrl = ResolveBaseUrl();
        var txt = _sitemapService.GenerateRobotsTxt(baseUrl);
        return Content(txt, "text/plain", Encoding.UTF8);
    }

    private string ResolveBaseUrl()
        => _seoOptions.SiteBaseUrl.TrimEnd('/');
}
