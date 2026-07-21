using System.Text;
using KidsParadiseByShoptick.Application.DTOs;
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
    private readonly IGoogleMerchantFeedService _merchantFeed;
    private readonly IGoogleMerchantFeedCache _merchantFeedCache;
    private readonly SeoOptions _seoOptions;
    private readonly IMemoryCache _cache;

    public SeoController(
        ISitemapService sitemapService,
        IGoogleMerchantFeedService merchantFeed,
        IGoogleMerchantFeedCache merchantFeedCache,
        IOptions<SeoOptions> seoOptions,
        IMemoryCache cache)
    {
        _sitemapService = sitemapService;
        _merchantFeed = merchantFeed;
        _merchantFeedCache = merchantFeedCache;
        _seoOptions = seoOptions.Value;
        _cache = cache;
    }

    [HttpGet("/sitemap.xml")]
    [Produces("application/xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = ResolveBaseUrl();
            var cacheKey = $"sitemap-xml:{baseUrl}:v{_merchantFeedCache.Version}";

            if (!_cache.TryGetValue(cacheKey, out string? xml))
            {
                xml = await _sitemapService.GenerateSitemapXmlAsync(baseUrl, cancellationToken);
                _cache.Set(cacheKey, xml, TimeSpan.FromMinutes(_seoOptions.SitemapCacheMinutes));
            }

            return Content(xml!, "application/xml; charset=utf-8", Encoding.UTF8);
        }
        catch (Exception)
        {
            return StatusCode(503, "Sitemap temporarily unavailable.");
        }
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

    /// <summary>
    /// Google Merchant Center primary product feed. Register this URL once in Merchant Center.
    /// Toy create/update/delete/sold invalidates cache so the next fetch is fresh.
    /// </summary>
    [HttpGet("/google-merchant-feed.xml")]
    [Produces("application/xml")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GoogleMerchantFeed(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = ResolveBaseUrl();
            var cacheKey = $"google-merchant-feed:{baseUrl}:v{_merchantFeedCache.Version}";
            var minutes = Math.Clamp(_seoOptions.MerchantFeedCacheMinutes, 1, 1440);

            if (!_cache.TryGetValue(cacheKey, out string? xml))
            {
                xml = await _merchantFeed.GenerateFeedXmlAsync(baseUrl, cancellationToken);
                _cache.Set(cacheKey, xml, TimeSpan.FromMinutes(minutes));
            }

            Response.Headers.CacheControl = "public, max-age=300";
            return Content(xml!, "application/xml; charset=utf-8", Encoding.UTF8);
        }
        catch (Exception)
        {
            return StatusCode(503, "Google Merchant feed temporarily unavailable.");
        }
    }

    [HttpGet("/api/seo/config")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public ActionResult<SeoPublicConfigDto> Config()
        => Ok(new SeoPublicConfigDto(
            _seoOptions.SiteName,
            _seoOptions.SiteBaseUrl.TrimEnd('/'),
            _seoOptions.DefaultTitle,
            _seoOptions.DefaultDescription,
            _seoOptions.DefaultKeywords,
            _seoOptions.DefaultOgImageUrl,
            _seoOptions.Locale,
            _seoOptions.Region));

    private string ResolveBaseUrl()
        => _seoOptions.SiteBaseUrl.TrimEnd('/');
}
