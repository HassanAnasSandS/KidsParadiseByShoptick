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

    public bool IsConfigured =>
        _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.FacebookPageId)
        && (!string.IsNullOrWhiteSpace(LoadStore().PageAccessToken)
            || !string.IsNullOrWhiteSpace(_options.PageAccessToken)
            || !string.IsNullOrWhiteSpace(LoadStore().LongLivedUserToken)
            || !string.IsNullOrWhiteSpace(_options.LongLivedUserToken));

    public async Task<MetaPageCredentials> EnsureCredentialsAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var store = LoadStore();
            var pageId = FirstNonEmpty(store.FacebookPageId, _options.FacebookPageId);
            var igId = FirstNonEmpty(store.InstagramBusinessAccountId, _options.InstagramBusinessAccountId);
            var pageToken = FirstNonEmpty(store.PageAccessToken, _options.PageAccessToken);
            var userToken = FirstNonEmpty(store.LongLivedUserToken, _options.LongLivedUserToken);

            if (string.IsNullOrWhiteSpace(pageId))
                throw new InvalidOperationException("Facebook Page ID is not configured.");

            if (!string.IsNullOrWhiteSpace(pageToken) && await IsPageTokenValidAsync(pageToken, cancellationToken))
                return new MetaPageCredentials(pageId, pageToken, NullIfEmpty(igId));

            if (!string.IsNullOrWhiteSpace(userToken))
            {
                _logger.LogInformation("Meta page token expired or missing. Refreshing from stored user token.");
                var refreshed = await FetchPageCredentialsAsync(userToken, pageId, cancellationToken);
                igId = FirstNonEmpty(refreshed.InstagramBusinessAccountId, igId);
                await SaveStoreAsync(userToken, refreshed, cancellationToken);
                return refreshed with { InstagramBusinessAccountId = NullIfEmpty(igId) };
            }

            throw new InvalidOperationException(
                "Facebook/Instagram access token expired. Generate a new token in Graph API Explorer and reconnect using POST /api/admin/meta/connect.");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<MetaPageCredentials> ConnectAsync(string userAccessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userAccessToken))
            throw new InvalidOperationException("User access token is required.");

        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret))
            throw new InvalidOperationException("Meta AppId and AppSecret must be configured in appsettings.Secrets.json.");

        var pageId = _options.FacebookPageId;
        if (string.IsNullOrWhiteSpace(pageId))
            throw new InvalidOperationException("FacebookPageId must be configured before connecting Meta.");

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var longLivedUserToken = await ExchangeForLongLivedUserTokenAsync(userAccessToken.Trim(), cancellationToken);
            var credentials = await FetchPageCredentialsAsync(longLivedUserToken, pageId, cancellationToken);

            if (string.IsNullOrWhiteSpace(credentials.InstagramBusinessAccountId)
                && !string.IsNullOrWhiteSpace(_options.InstagramBusinessAccountId))
            {
                credentials = credentials with { InstagramBusinessAccountId = _options.InstagramBusinessAccountId };
            }

            await SaveStoreAsync(longLivedUserToken, credentials, cancellationToken);
            _logger.LogInformation("Meta Facebook/Instagram connected for page {PageId}", credentials.FacebookPageId);
            return credentials;
        }
        finally
        {
            _refreshLock.Release();
        }
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
        var accountsUrl = $"{GraphBase}/me/accounts?access_token={Uri.EscapeDataString(userAccessToken)}";
        using var accountsResponse = await _http.GetAsync(accountsUrl, cancellationToken);
        var accountsBody = await accountsResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!accountsResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Meta me/accounts failed: {ParseGraphError(accountsBody)}");

        using var accountsDoc = JsonDocument.Parse(accountsBody);
        if (!accountsDoc.RootElement.TryGetProperty("data", out var data))
            throw new InvalidOperationException("Meta did not return any Facebook Pages for this account.");

        string? pageToken = null;
        foreach (var page in data.EnumerateArray())
        {
            if (!page.TryGetProperty("id", out var idEl) || idEl.GetString() != pageId)
                continue;

            pageToken = page.TryGetProperty("access_token", out var tokenEl) ? tokenEl.GetString() : null;
            break;
        }

        if (string.IsNullOrWhiteSpace(pageToken))
            throw new InvalidOperationException($"Facebook Page {pageId} was not found for the authorized Meta account.");

        var pageUrl =
            $"{GraphBase}/{pageId}?fields=instagram_business_account&access_token={Uri.EscapeDataString(pageToken)}";
        using var pageResponse = await _http.GetAsync(pageUrl, cancellationToken);
        var pageBody = await pageResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!pageResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Meta page lookup failed: {ParseGraphError(pageBody)}");

        string? igId = null;
        using (var pageDoc = JsonDocument.Parse(pageBody))
        {
            if (pageDoc.RootElement.TryGetProperty("instagram_business_account", out var igEl)
                && igEl.TryGetProperty("id", out var igIdEl))
            {
                igId = igIdEl.GetString();
            }
        }

        return new MetaPageCredentials(pageId, pageToken, igId);
    }

    async Task<bool> IsPageTokenValidAsync(string pageToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            // Without app credentials we cannot debug_token; assume configured token is valid.
            return true;
        }

        try
        {
            var appToken = $"{_options.AppId}|{_options.AppSecret}";
            var url =
                $"{GraphBase}/debug_token?input_token={Uri.EscapeDataString(pageToken)}" +
                $"&access_token={Uri.EscapeDataString(appToken)}";

            using var response = await _http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false;

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out var data))
                return false;

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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Meta debug_token check failed");
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

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
