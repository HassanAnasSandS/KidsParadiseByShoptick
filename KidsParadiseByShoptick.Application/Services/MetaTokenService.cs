using System.Text.Json;
using System.Text.Json.Serialization;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class MetaTokenService : IMetaTokenService
{
    private const string GraphBase = "https://graph.facebook.com/v21.0";
    /// <summary>Mahirah portfolio catalog — never use for Kids Paradise WhatsApp.</summary>
    private const string LegacyMahirahCatalogId = "234921813846539";

    private readonly MetaSocialOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<MetaTokenService> _logger;
    private readonly string _tokenFilePath;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public MetaTokenService(
        IOptions<MetaSocialOptions> options,
        IConfiguration configuration,
        HttpClient http,
        ILogger<MetaTokenService> logger)
    {
        _options = options.Value;
        _http = http;
        _logger = logger;

        var basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "KidsParadiseByShoptick.Published");
        basePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(basePath);
        _tokenFilePath = Path.Combine(basePath, ".app-data", "meta-oauth.json");
    }

    public bool IsConfigured
    {
        get
        {
            if (!_options.Enabled)
                return false;

            var store = LoadStore();
            var pageId = FirstNonEmpty(store.FacebookPageId, _options.FacebookPageId);
            if (string.IsNullOrWhiteSpace(pageId))
                return false;

            return !string.IsNullOrWhiteSpace(FirstNonEmpty(store.PageAccessToken, _options.PageAccessToken))
                || !string.IsNullOrWhiteSpace(FirstNonEmpty(store.LongLivedUserToken, _options.LongLivedUserToken));
        }
    }

    public async Task<MetaPageCredentials> EnsureCredentialsAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var store = LoadStore();
            var pageId = FirstNonEmpty(store.FacebookPageId, _options.FacebookPageId);
            var igId = FirstNonEmpty(store.InstagramBusinessAccountId, _options.InstagramBusinessAccountId);
            var catalogId = FirstNonEmpty(_options.WhatsAppCatalogId, store.WhatsAppCatalogId);
            var wabaId = FirstNonEmpty(_options.WhatsAppBusinessAccountId, store.WhatsAppBusinessAccountId);
            var pageToken = FirstNonEmpty(store.PageAccessToken, _options.PageAccessToken);
            var userToken = FirstNonEmpty(store.LongLivedUserToken, _options.LongLivedUserToken);

            if (string.IsNullOrWhiteSpace(pageId))
                throw new InvalidOperationException("Facebook Page ID is not configured.");

            MetaPageCredentials credentials;
            if (!string.IsNullOrWhiteSpace(pageToken) && await IsPageTokenValidAsync(pageToken, cancellationToken))
            {
                credentials = new MetaPageCredentials(
                    pageId, pageToken, NullIfEmpty(igId), NullIfEmpty(wabaId), NullIfEmpty(catalogId));
            }
            else if (!string.IsNullOrWhiteSpace(userToken))
            {
                _logger.LogInformation("Meta page token expired or missing. Refreshing from stored user token.");
                credentials = await FetchPageCredentialsAsync(userToken, pageId, cancellationToken);
                igId = FirstNonEmpty(credentials.InstagramBusinessAccountId, igId);
                wabaId = FirstNonEmpty(_options.WhatsAppBusinessAccountId, wabaId);
                catalogId = FirstNonEmpty(_options.WhatsAppCatalogId, catalogId);
                credentials = credentials with
                {
                    InstagramBusinessAccountId = NullIfEmpty(igId),
                    WhatsAppBusinessAccountId = NullIfEmpty(wabaId),
                    WhatsAppCatalogId = NullIfEmpty(catalogId),
                };
                await SaveStoreAsync(userToken, credentials, cancellationToken);
                return credentials;
            }
            else
            {
                throw new InvalidOperationException(
                    "Facebook/Instagram access token expired. Generate a new token in Graph API Explorer and reconnect using POST /api/admin/meta/connect.");
            }

            if (string.IsNullOrWhiteSpace(credentials.WhatsAppCatalogId)
                && !HasConfiguredCatalog())
            {
                var discovered = await DiscoverWhatsAppCatalogAsync(
                    credentials.FacebookPageId, credentials.PageAccessToken, cancellationToken);
                if (!string.IsNullOrWhiteSpace(discovered.CatalogId))
                {
                    credentials = credentials with
                    {
                        WhatsAppBusinessAccountId = NullIfEmpty(discovered.WabaId),
                        WhatsAppCatalogId = discovered.CatalogId,
                    };
                    if (!string.IsNullOrWhiteSpace(userToken))
                        await SaveStoreAsync(userToken, credentials, cancellationToken);
                }
            }

            return credentials;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<MetaPageCredentials> ConnectAsync(
        MetaConnectRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserAccessToken) && string.IsNullOrWhiteSpace(request.PageAccessToken))
            throw new InvalidOperationException("A user or page access token is required.");

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(request.PageAccessToken)
                && !string.IsNullOrWhiteSpace(request.FacebookPageId))
            {
                var direct = await BuildDirectPageCredentialsAsync(request, cancellationToken);
                var directWabaId = FirstNonEmpty(
                    request.WhatsAppBusinessAccountId,
                    _options.WhatsAppBusinessAccountId,
                    direct.WhatsAppBusinessAccountId);
                var directCatalogId = FirstNonEmpty(
                    request.WhatsAppCatalogId,
                    _options.WhatsAppCatalogId,
                    direct.WhatsAppCatalogId);
                if (!string.IsNullOrWhiteSpace(directWabaId) || !string.IsNullOrWhiteSpace(directCatalogId))
                {
                    direct = direct with
                    {
                        WhatsAppBusinessAccountId = NullIfEmpty(directWabaId),
                        WhatsAppCatalogId = NullIfEmpty(directCatalogId),
                    };
                }

                if (string.IsNullOrWhiteSpace(direct.WhatsAppCatalogId)
                    && !HasConfiguredCatalog())
                {
                    var discovered = await DiscoverWhatsAppCatalogAsync(
                        direct.FacebookPageId, direct.PageAccessToken, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(discovered.CatalogId))
                    {
                        direct = direct with
                        {
                            WhatsAppBusinessAccountId = NullIfEmpty(discovered.WabaId),
                            WhatsAppCatalogId = discovered.CatalogId,
                        };
                    }
                }

                var storedUserToken = FirstNonEmpty(request.UserAccessToken, request.PageAccessToken)!;
                await SaveStoreAsync(storedUserToken.Trim(), direct, cancellationToken);
                _logger.LogInformation("Meta connected with supplied page token for {PageId}", direct.FacebookPageId);
                return direct;
            }

            var userToken = request.UserAccessToken.Trim();
            var pageId = FirstNonEmpty(request.FacebookPageId, _options.FacebookPageId);
            var userTokenForStore = userToken;

            if (!string.IsNullOrWhiteSpace(_options.AppId) && !string.IsNullOrWhiteSpace(_options.AppSecret))
            {
                userTokenForStore = await ExchangeForLongLivedUserTokenAsync(userToken, cancellationToken);
            }
            else
            {
                _logger.LogWarning(
                    "Meta AppSecret is not configured. Storing the short-lived user token until AppSecret is added.");
            }

            MetaPageCredentials credentials;
            if (string.IsNullOrWhiteSpace(pageId))
            {
                credentials = await DiscoverPageCredentialsAsync(userTokenForStore, cancellationToken);
            }
            else
            {
                credentials = await FetchPageCredentialsAsync(userTokenForStore, pageId, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(credentials.InstagramBusinessAccountId))
            {
                var igId = FirstNonEmpty(request.InstagramBusinessAccountId, _options.InstagramBusinessAccountId);
                if (!string.IsNullOrWhiteSpace(igId))
                    credentials = credentials with { InstagramBusinessAccountId = igId };
            }

            var wabaId = FirstNonEmpty(request.WhatsAppBusinessAccountId, _options.WhatsAppBusinessAccountId);
            var catalogId = FirstNonEmpty(request.WhatsAppCatalogId, _options.WhatsAppCatalogId);
            if (!string.IsNullOrWhiteSpace(wabaId) || !string.IsNullOrWhiteSpace(catalogId))
            {
                credentials = credentials with
                {
                    WhatsAppBusinessAccountId = NullIfEmpty(wabaId),
                    WhatsAppCatalogId = NullIfEmpty(catalogId),
                };
            }

            if (string.IsNullOrWhiteSpace(credentials.WhatsAppCatalogId)
                && !HasConfiguredCatalog())
            {
                var discovered = await DiscoverWhatsAppCatalogAsync(
                    credentials.FacebookPageId, credentials.PageAccessToken, cancellationToken);
                if (!string.IsNullOrWhiteSpace(discovered.CatalogId))
                {
                    credentials = credentials with
                    {
                        WhatsAppBusinessAccountId = NullIfEmpty(discovered.WabaId),
                        WhatsAppCatalogId = discovered.CatalogId,
                    };
                }
            }

            await SaveStoreAsync(userTokenForStore, credentials, cancellationToken);
            _logger.LogInformation("Meta Facebook/Instagram connected for page {PageId}", credentials.FacebookPageId);
            return credentials;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    async Task<MetaPageCredentials> BuildDirectPageCredentialsAsync(
        MetaConnectRequest request, CancellationToken cancellationToken)
    {
        var pageId = request.FacebookPageId!.Trim();
        var pageToken = request.PageAccessToken!.Trim();
        var igId = NullIfEmpty(FirstNonEmpty(request.InstagramBusinessAccountId, _options.InstagramBusinessAccountId));

        if (!string.IsNullOrWhiteSpace(igId))
            return new MetaPageCredentials(pageId, pageToken, igId, null, null);

        var pageUrl =
            $"{GraphBase}/{pageId}?fields=instagram_business_account,whatsapp_business_account&access_token={Uri.EscapeDataString(pageToken)}";
        using var pageResponse = await _http.GetAsync(pageUrl, cancellationToken);
        var pageBody = await pageResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!pageResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Meta page lookup failed: {ParseGraphError(pageBody)}");

        using var pageDoc = JsonDocument.Parse(pageBody);
        if (pageDoc.RootElement.TryGetProperty("instagram_business_account", out var igEl)
            && igEl.TryGetProperty("id", out var igIdEl))
        {
            igId = igIdEl.GetString();
        }

        string? wabaId = null;
        if (pageDoc.RootElement.TryGetProperty("whatsapp_business_account", out var wabaEl)
            && wabaEl.TryGetProperty("id", out var wabaIdEl))
        {
            wabaId = wabaIdEl.GetString();
        }

        string? catalogId = null;
        if (!string.IsNullOrWhiteSpace(wabaId))
            catalogId = await FetchWhatsAppCatalogIdAsync(wabaId, pageToken, cancellationToken);

        return new MetaPageCredentials(pageId, pageToken, NullIfEmpty(igId), NullIfEmpty(wabaId), NullIfEmpty(catalogId));
    }

    async Task<MetaPageCredentials> DiscoverPageCredentialsAsync(
        string userAccessToken, CancellationToken cancellationToken)
    {
        var accountsUrl =
            $"{GraphBase}/me/accounts?fields=id,name,access_token,instagram_business_account{{id,username}}&access_token={Uri.EscapeDataString(userAccessToken)}";
        using var accountsResponse = await _http.GetAsync(accountsUrl, cancellationToken);
        var accountsBody = await accountsResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!accountsResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Meta me/accounts failed: {ParseGraphError(accountsBody)}");

        using var accountsDoc = JsonDocument.Parse(accountsBody);
        if (!accountsDoc.RootElement.TryGetProperty("data", out var data))
            throw new InvalidOperationException("Meta did not return any Facebook Pages for this account.");

        var pages = data.EnumerateArray().ToList();
        if (pages.Count == 0)
        {
            var configuredPageId = FirstNonEmpty(_options.FacebookPageId);
            if (!string.IsNullOrWhiteSpace(configuredPageId))
                return await FetchPageCredentialsAsync(userAccessToken, configuredPageId, cancellationToken);

            throw new InvalidOperationException(
                "No Facebook Pages were granted to this token. In Graph API Explorer, click Generate Access Token again, " +
                "approve all requested permissions, and select your Kids Paradise Facebook Page in the popup.");
        }

        JsonElement? selected = null;
        foreach (var page in pages)
        {
            if (IsPreferredKidsParadisePage(page))
            {
                selected = page;
                break;
            }
        }

        selected ??= pages[0];
        var pageId = selected.Value.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Meta returned a page without an id.");
        var pageToken = selected.Value.TryGetProperty("access_token", out var tokenEl) ? tokenEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(pageToken))
            throw new InvalidOperationException($"Meta did not return a page access token for page {pageId}.");

        string? igId = null;
        if (selected.Value.TryGetProperty("instagram_business_account", out var igEl)
            && igEl.TryGetProperty("id", out var igIdEl))
        {
            igId = igIdEl.GetString();
        }

        if (string.IsNullOrWhiteSpace(igId))
        {
            var pageUrl =
                $"{GraphBase}/{pageId}?fields=instagram_business_account,whatsapp_business_account&access_token={Uri.EscapeDataString(pageToken)}";
            using var pageResponse = await _http.GetAsync(pageUrl, cancellationToken);
            var pageBody = await pageResponse.Content.ReadAsStringAsync(cancellationToken);
            if (pageResponse.IsSuccessStatusCode)
            {
                using var pageDoc = JsonDocument.Parse(pageBody);
                if (pageDoc.RootElement.TryGetProperty("instagram_business_account", out var nestedIg)
                    && nestedIg.TryGetProperty("id", out var nestedIgId))
                {
                    igId = nestedIgId.GetString();
                }
            }
        }

        var discovered = await DiscoverWhatsAppCatalogAsync(pageId, pageToken, cancellationToken);
        var catalogId = HasConfiguredCatalog()
            ? null
            : discovered.CatalogId;
        return new MetaPageCredentials(
            pageId,
            pageToken,
            NullIfEmpty(igId),
            NullIfEmpty(discovered.WabaId),
            NullIfEmpty(catalogId));
    }

    async Task<(string? WabaId, string? CatalogId)> DiscoverWhatsAppCatalogAsync(
        string pageId, string pageToken, CancellationToken cancellationToken)
    {
        try
        {
            string? wabaId = null;
            string? catalogId = null;

            var pageUrl =
                $"{GraphBase}/{pageId}?fields=whatsapp_business_account{{id}}&access_token={Uri.EscapeDataString(pageToken)}";
            using var pageResponse = await _http.GetAsync(pageUrl, cancellationToken);
            var pageBody = await pageResponse.Content.ReadAsStringAsync(cancellationToken);
            if (pageResponse.IsSuccessStatusCode)
            {
                using var pageDoc = JsonDocument.Parse(pageBody);
                if (pageDoc.RootElement.TryGetProperty("whatsapp_business_account", out var wabaEl)
                    && wabaEl.TryGetProperty("id", out var wabaIdEl))
                {
                    wabaId = wabaIdEl.GetString();
                    if (!string.IsNullOrWhiteSpace(wabaId))
                        catalogId = await FetchWhatsAppCatalogIdAsync(wabaId, pageToken, cancellationToken);
                }
            }

            if (string.IsNullOrWhiteSpace(catalogId))
                catalogId = await FetchPageProductCatalogIdAsync(pageId, pageToken, cancellationToken);

            return (NullIfEmpty(wabaId), NullIfEmpty(RejectLegacyCatalog(catalogId)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not discover WhatsApp Business catalog for page {PageId}", pageId);
            return (null, null);
        }
    }

    async Task<string?> FetchPageProductCatalogIdAsync(
        string pageId, string pageToken, CancellationToken cancellationToken)
    {
        var url =
            $"{GraphBase}/{pageId}?fields=product_catalogs{{id}}&access_token={Uri.EscapeDataString(pageToken)}";
        using var response = await _http.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("product_catalogs", out var catalogsEl)
            || !catalogsEl.TryGetProperty("data", out var data))
        {
            return null;
        }

        foreach (var catalog in data.EnumerateArray())
        {
            if (catalog.TryGetProperty("id", out var idEl))
            {
                var id = idEl.GetString();
                if (!string.IsNullOrWhiteSpace(id) && !IsLegacyCatalog(id))
                    return id;
            }
        }

        return null;
    }

    async Task<string?> FetchWhatsAppCatalogIdAsync(
        string wabaId, string accessToken, CancellationToken cancellationToken)
    {
        var wabaUrl =
            $"{GraphBase}/{wabaId}?fields=product_catalog{{id}}&access_token={Uri.EscapeDataString(accessToken)}";
        using var wabaResponse = await _http.GetAsync(wabaUrl, cancellationToken);
        var wabaBody = await wabaResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!wabaResponse.IsSuccessStatusCode)
            return null;

        using var wabaDoc = JsonDocument.Parse(wabaBody);
        if (wabaDoc.RootElement.TryGetProperty("product_catalog", out var catalogEl)
            && catalogEl.TryGetProperty("id", out var catalogIdEl))
        {
            return RejectLegacyCatalog(catalogIdEl.GetString());
        }

        var catalogsUrl =
            $"{GraphBase}/{wabaId}/product_catalogs?fields=id&access_token={Uri.EscapeDataString(accessToken)}";
        using var catalogsResponse = await _http.GetAsync(catalogsUrl, cancellationToken);
        var catalogsBody = await catalogsResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!catalogsResponse.IsSuccessStatusCode)
            return null;

        using var catalogsDoc = JsonDocument.Parse(catalogsBody);
        if (!catalogsDoc.RootElement.TryGetProperty("data", out var data))
            return null;

        foreach (var catalog in data.EnumerateArray())
        {
            if (catalog.TryGetProperty("id", out var idEl))
            {
                var id = idEl.GetString();
                if (!string.IsNullOrWhiteSpace(id) && !IsLegacyCatalog(id))
                    return id;
            }
        }

        return null;
    }

    static bool IsLegacyCatalog(string catalogId) =>
        string.Equals(catalogId, LegacyMahirahCatalogId, StringComparison.Ordinal);

    static string? RejectLegacyCatalog(string? catalogId) =>
        catalogId is not null && IsLegacyCatalog(catalogId) ? null : catalogId;

    static bool IsPreferredKidsParadisePage(JsonElement page)
    {
        if (!page.TryGetProperty("name", out var nameEl))
            return false;

        var name = nameEl.GetString() ?? string.Empty;
        return name.Contains("paradise", StringComparison.OrdinalIgnoreCase)
            || name.Contains("closet", StringComparison.OrdinalIgnoreCase)
            || name.Contains("toy", StringComparison.OrdinalIgnoreCase)
            || name.Contains("kids", StringComparison.OrdinalIgnoreCase);
    }

    async Task<string> ExchangeForLongLivedUserTokenAsync(string shortLivedToken, CancellationToken cancellationToken)
    {
        var url =
            $"{GraphBase}/oauth/access_token?grant_type=fb_exchange_token" +
            $"&client_id={Uri.EscapeDataString(_options.AppId)}" +
            $"&client_secret={Uri.EscapeDataString(_options.AppSecret)}" +
            $"&fb_exchange_token={Uri.EscapeDataString(shortLivedToken)}";

        using var response = await _http.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Meta token exchange failed: {ParseGraphError(body)}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Meta token exchange did not return an access token.");
    }

    async Task<MetaPageCredentials> FetchPageCredentialsAsync(
        string userAccessToken, string pageId, CancellationToken cancellationToken)
    {
        string? pageToken = null;
        string? igId = null;

        var accountsUrl =
            $"{GraphBase}/me/accounts?fields=id,access_token&access_token={Uri.EscapeDataString(userAccessToken)}";
        using (var accountsResponse = await _http.GetAsync(accountsUrl, cancellationToken))
        {
            var accountsBody = await accountsResponse.Content.ReadAsStringAsync(cancellationToken);
            if (accountsResponse.IsSuccessStatusCode)
            {
                using var accountsDoc = JsonDocument.Parse(accountsBody);
                if (accountsDoc.RootElement.TryGetProperty("data", out var data))
                {
                    foreach (var page in data.EnumerateArray())
                    {
                        if (!page.TryGetProperty("id", out var idEl) || idEl.GetString() != pageId)
                            continue;

                        pageToken = page.TryGetProperty("access_token", out var tokenEl) ? tokenEl.GetString() : null;
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(pageToken))
        {
            var directUrl =
                $"{GraphBase}/{pageId}?fields=id,name,access_token,instagram_business_account{{id,username}}" +
                $"&access_token={Uri.EscapeDataString(userAccessToken)}";
            using var directResponse = await _http.GetAsync(directUrl, cancellationToken);
            var directBody = await directResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!directResponse.IsSuccessStatusCode)
                throw new InvalidOperationException($"Meta page lookup failed: {ParseGraphError(directBody)}");

            using var directDoc = JsonDocument.Parse(directBody);
            pageToken = directDoc.RootElement.TryGetProperty("access_token", out var tokenEl)
                ? tokenEl.GetString()
                : null;
            if (directDoc.RootElement.TryGetProperty("instagram_business_account", out var igEl)
                && igEl.TryGetProperty("id", out var igIdEl))
            {
                igId = igIdEl.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(pageToken))
            throw new InvalidOperationException($"Facebook Page {pageId} was not found for the authorized Meta account.");

        if (string.IsNullOrWhiteSpace(igId))
        {
            var pageUrl =
                $"{GraphBase}/{pageId}?fields=instagram_business_account,whatsapp_business_account&access_token={Uri.EscapeDataString(pageToken)}";
            using var pageResponse = await _http.GetAsync(pageUrl, cancellationToken);
            var pageBody = await pageResponse.Content.ReadAsStringAsync(cancellationToken);
            if (pageResponse.IsSuccessStatusCode)
            {
                using var pageDoc = JsonDocument.Parse(pageBody);
                if (pageDoc.RootElement.TryGetProperty("instagram_business_account", out var igEl)
                    && igEl.TryGetProperty("id", out var igIdEl))
                {
                    igId = igIdEl.GetString();
                }
            }
        }

        var discovered = await DiscoverWhatsAppCatalogAsync(pageId, pageToken, cancellationToken);
        var catalogId = HasConfiguredCatalog()
            ? null
            : discovered.CatalogId;
        return new MetaPageCredentials(
            pageId,
            pageToken,
            NullIfEmpty(igId),
            NullIfEmpty(discovered.WabaId),
            NullIfEmpty(catalogId));
    }

    bool HasConfiguredCatalog() =>
        !string.IsNullOrWhiteSpace(_options.WhatsAppCatalogId);

    async Task<bool> IsPageTokenValidAsync(string pageToken, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.AppId) && !string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            try
            {
                var appToken = $"{_options.AppId}|{_options.AppSecret}";
                var debugUrl =
                    $"{GraphBase}/debug_token?input_token={Uri.EscapeDataString(pageToken)}" +
                    $"&access_token={Uri.EscapeDataString(appToken)}";

                using var debugResponse = await _http.GetAsync(debugUrl, cancellationToken);
                var debugBody = await debugResponse.Content.ReadAsStringAsync(cancellationToken);
                if (debugResponse.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(debugBody);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        if (data.TryGetProperty("is_valid", out var validEl) && !validEl.GetBoolean())
                            return false;

                        if (data.TryGetProperty("expires_at", out var expiresEl))
                        {
                            var expiresAt = expiresEl.GetInt64();
                            if (expiresAt > 0)
                            {
                                var expiry = DateTimeOffset.FromUnixTimeSeconds(expiresAt);
                                if (expiry <= DateTimeOffset.UtcNow.AddMinutes(5))
                                    return false;
                            }
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Meta debug_token check failed; falling back to live page probe");
            }
        }

        try
        {
            var probeUrl = $"{GraphBase}/me?fields=id&access_token={Uri.EscapeDataString(pageToken)}";
            using var response = await _http.GetAsync(probeUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Meta page token probe failed");
            return false;
        }
    }

    public async Task<bool> TryMaintainAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return false;

        try
        {
            await EnsureCredentialsAsync(cancellationToken);

            var store = LoadStore();
            var userToken = FirstNonEmpty(store.LongLivedUserToken, _options.LongLivedUserToken);
            if (!string.IsNullOrWhiteSpace(userToken)
                && !string.IsNullOrWhiteSpace(_options.AppId)
                && !string.IsNullOrWhiteSpace(_options.AppSecret))
            {
            var refreshedUserToken = await ExchangeForLongLivedUserTokenAsync(userToken, cancellationToken);
            var pageId = FirstNonEmpty(store.FacebookPageId, _options.FacebookPageId)!;
            var credentials = await FetchPageCredentialsAsync(refreshedUserToken, pageId, cancellationToken);
            var igId = FirstNonEmpty(
                credentials.InstagramBusinessAccountId,
                store.InstagramBusinessAccountId,
                _options.InstagramBusinessAccountId);
            var catalogId = FirstNonEmpty(_options.WhatsAppCatalogId, store.WhatsAppCatalogId);
            var wabaId = FirstNonEmpty(
                _options.WhatsAppBusinessAccountId,
                store.WhatsAppBusinessAccountId);
            await SaveStoreAsync(
                refreshedUserToken,
                credentials with
                {
                    InstagramBusinessAccountId = NullIfEmpty(igId),
                    WhatsAppBusinessAccountId = NullIfEmpty(wabaId),
                    WhatsAppCatalogId = NullIfEmpty(catalogId),
                },
                cancellationToken);
            _logger.LogInformation("Meta long-lived user token extended.");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Meta token maintenance failed");
            return false;
        }
    }

    MetaTokenStore LoadStore()
    {
        if (!File.Exists(_tokenFilePath))
            return new MetaTokenStore();

        try
        {
            var json = File.ReadAllText(_tokenFilePath);
            return JsonSerializer.Deserialize<MetaTokenStore>(json) ?? new MetaTokenStore();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read Meta token file");
            return new MetaTokenStore();
        }
    }

    async Task SaveStoreAsync(
        string longLivedUserToken, MetaPageCredentials credentials, CancellationToken cancellationToken)
    {
        var store = new MetaTokenStore
        {
            LongLivedUserToken = longLivedUserToken,
            PageAccessToken = credentials.PageAccessToken,
            FacebookPageId = credentials.FacebookPageId,
            InstagramBusinessAccountId = credentials.InstagramBusinessAccountId,
            WhatsAppBusinessAccountId = credentials.WhatsAppBusinessAccountId,
            WhatsAppCatalogId = credentials.WhatsAppCatalogId,
            UpdatedAt = DateTime.UtcNow,
        };

        var dir = Path.GetDirectoryName(_tokenFilePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_tokenFilePath, json, cancellationToken);
    }

    static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    static string ParseGraphError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message))
                return message.GetString() ?? body;
        }
        catch
        {
            // ignored
        }

        return body;
    }

    sealed class MetaTokenStore
    {
        [JsonPropertyName("longLivedUserToken")]
        public string? LongLivedUserToken { get; set; }

        [JsonPropertyName("pageAccessToken")]
        public string? PageAccessToken { get; set; }

        [JsonPropertyName("facebookPageId")]
        public string? FacebookPageId { get; set; }

        [JsonPropertyName("instagramBusinessAccountId")]
        public string? InstagramBusinessAccountId { get; set; }

        [JsonPropertyName("whatsAppBusinessAccountId")]
        public string? WhatsAppBusinessAccountId { get; set; }

        [JsonPropertyName("whatsAppCatalogId")]
        public string? WhatsAppCatalogId { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
