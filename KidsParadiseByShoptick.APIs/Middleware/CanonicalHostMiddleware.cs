using KidsParadiseByShoptick.Application.Options;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.APIs.Middleware;

/// <summary>
/// Permanently redirects to the canonical www + HTTPS URL only.
/// Never strips www. Uses the request Host header (not X-Forwarded-Host) to avoid reverse redirects.
/// </summary>
public class CanonicalHostMiddleware
{
    private const string WwwHost = "www.kidsparadisethetoyshop.store";

    private readonly RequestDelegate _next;
    private readonly SeoOptions _options;

    public CanonicalHostMiddleware(RequestDelegate next, IOptions<SeoOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.EnforceCanonicalHost)
        {
            await _next(context);
            return;
        }

        var canonicalHost = NormalizeHost(_options.CanonicalHost);
        if (string.IsNullOrEmpty(canonicalHost))
            canonicalHost = WwwHost;

        // Safety: canonical must be www — never allow apex as redirect target.
        if (!canonicalHost.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            canonicalHost = WwwHost;

        var requestHost = NormalizeHost(context.Request.Host.Host);
        var scheme = GetRequestScheme(context);

        var isCanonical = scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
            && requestHost.Equals(canonicalHost, StringComparison.OrdinalIgnoreCase);

        if (isCanonical)
        {
            await _next(context);
            return;
        }

        // 301 → https://www... only (never www → apex)
        var path = context.Request.PathBase + context.Request.Path;
        var query = context.Request.QueryString.Value ?? string.Empty;
        var location = $"https://{canonicalHost}{path}{query}";

        context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
        context.Response.Headers.Location = location;
    }

    private static string GetRequestScheme(HttpContext context)
    {
        // Trust forwarded proto only (TLS terminator). Do NOT trust X-Forwarded-Host.
        var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedProto))
            return forwardedProto.Split(',')[0].Trim();

        return context.Request.Scheme;
    }

    private static string NormalizeHost(string? host)
        => (host ?? string.Empty).Trim().ToLowerInvariant().Split(':')[0];
}
