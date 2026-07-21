using System.Text.Json;
using System.Text.Json.Serialization;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class TikTokAuthService : ITikTokAuthService
{
    private const string AuthUri = "https://www.tiktok.com/v2/auth/authorize/";
    private const string TokenUri = "https://open.tiktokapis.com/v2/oauth/token/";
    private const string CacheKeyPrefix = "tiktok-oauth-state:";

    private readonly TikTokSocialOptions _options;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _http;
    private readonly ILogger<TikTokAuthService> _logger;
    private readonly string _tokenFilePath;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public TikTokAuthService(
        IOptions<TikTokSocialOptions> options,
        IMemoryCache cache,
        IConfiguration configuration,
        HttpClient http,
        ILogger<TikTokAuthService> logger)
    {
        _options = options.Value;
        _cache = cache;
        _http = http;
        _logger = logger;

        var basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "KidsParadiseByShoptick.Published");
        basePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(basePath);
        _tokenFilePath = Path.Combine(basePath, ".app-data", "tiktok-oauth.json");
    }

    public bool IsOAuthConfigured =>
        _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.ClientKey)
        && !string.IsNullOrWhiteSpace(_options.ClientSecret);

    public bool IsConnected
    {
        get
        {
            var store = LoadStore();
            return !string.IsNullOrWhiteSpace(store.RefreshToken)
                || (!string.IsNullOrWhiteSpace(store.AccessToken) && store.AccessTokenExpiresAt > DateTimeOffset.UtcNow);
        }
    }

    public string BuildAuthorizationUrl(out string state)
    {
        EnsureOAuthConfigured();
        if (string.IsNullOrWhiteSpace(_options.RedirectUri))
            throw new InvalidOperationException("TikTokSocial:RedirectUri is not configured.");

        state = Guid.NewGuid().ToString("N");
        _cache.Set(CacheKeyPrefix + state, true, TimeSpan.FromMinutes(15));

        return AuthUri +
               $"?client_key={Uri.EscapeDataString(_options.ClientKey)}" +
               $"&scope={Uri.EscapeDataString(_options.Scopes)}" +
               "&response_type=code" +
               $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task CompleteAuthorizationAsync(string state, string code, CancellationToken cancellationToken = default)
    {
        // Prefer one-time cached state from Admin Connect; allow manual browser URL too.
        if (_cache.TryGetValue(CacheKeyPrefix + state, out bool _))
            _cache.Remove(CacheKeyPrefix + state);
        else if (string.IsNullOrWhiteSpace(state))
            throw new InvalidOperationException("OAuth state missing. Start again from Social Settings.");

        EnsureOAuthConfigured();

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_key"] = _options.ClientKey,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = _options.RedirectUri,
        });

        using var response = await _http.PostAsync(TokenUri, form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"TikTok token exchange failed: {body}");

        await SaveTokenResponseAsync(body, preserveRefreshIfMissing: false, cancellationToken);
    }

    public async Task<(string AccessToken, string OpenId)> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            var store = LoadStore();
            if (!string.IsNullOrWhiteSpace(store.AccessToken)
                && !string.IsNullOrWhiteSpace(store.OpenId)
                && store.AccessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                return (store.AccessToken, store.OpenId);
            }

            if (string.IsNullOrWhiteSpace(store.RefreshToken))
            {
                throw new InvalidOperationException(
                    "TikTok is not connected. Open Social Settings → Connect TikTok.");
            }

            await RefreshLockedAsync(store, cancellationToken);
            store = LoadStore();
            if (string.IsNullOrWhiteSpace(store.AccessToken) || string.IsNullOrWhiteSpace(store.OpenId))
                throw new InvalidOperationException("TikTok token refresh did not return access token/open_id.");

            return (store.AccessToken, store.OpenId);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return false;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            var store = LoadStore();
            if (string.IsNullOrWhiteSpace(store.RefreshToken))
                return false;

            await RefreshLockedAsync(store, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TikTok token refresh failed.");
            return false;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_tokenFilePath))
            File.Delete(_tokenFilePath);
        return Task.CompletedTask;
    }

    async Task RefreshLockedAsync(TokenStore store, CancellationToken cancellationToken)
    {
        EnsureOAuthConfigured();
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_key"] = _options.ClientKey,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = store.RefreshToken!,
        });

        using var response = await _http.PostAsync(TokenUri, form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"TikTok refresh failed: {body}");

        await SaveTokenResponseAsync(body, preserveRefreshIfMissing: true, cancellationToken);
    }

    async Task SaveTokenResponseAsync(string body, bool preserveRefreshIfMissing, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // TikTok may wrap in { data: {...} } or return flat token fields.
        var tokenRoot = root.TryGetProperty("data", out var dataEl) ? dataEl : root;

        var accessToken = tokenRoot.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        var refreshToken = tokenRoot.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var openId = tokenRoot.TryGetProperty("open_id", out var oid) ? oid.GetString() : null;
        var expiresIn = tokenRoot.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 86400;
        var refreshExpiresIn = tokenRoot.TryGetProperty("refresh_expires_in", out var rexp) ? rexp.GetInt32() : 0;

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(openId))
            throw new InvalidOperationException($"TikTok token response missing access_token/open_id: {body}");

        var existing = LoadStore();
        if (string.IsNullOrWhiteSpace(refreshToken) && preserveRefreshIfMissing)
            refreshToken = existing.RefreshToken;

        var store = new TokenStore
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            OpenId = openId,
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60)),
            RefreshTokenExpiresAt = refreshExpiresIn > 0
                ? DateTimeOffset.UtcNow.AddSeconds(refreshExpiresIn)
                : existing.RefreshTokenExpiresAt,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var json = JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_tokenFilePath, json, cancellationToken);
    }

    TokenStore LoadStore()
    {
        if (!File.Exists(_tokenFilePath))
            return new TokenStore();

        try
        {
            var json = File.ReadAllText(_tokenFilePath);
            return JsonSerializer.Deserialize<TokenStore>(json) ?? new TokenStore();
        }
        catch
        {
            return new TokenStore();
        }
    }

    void EnsureOAuthConfigured()
    {
        if (!IsOAuthConfigured)
        {
            throw new InvalidOperationException(
                "TikTok is not configured. Set TikTokSocial:Enabled, ClientKey, ClientSecret in appsettings/Secrets.");
        }
    }

    private sealed class TokenStore
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("openId")]
        public string? OpenId { get; set; }

        [JsonPropertyName("accessTokenExpiresAt")]
        public DateTimeOffset AccessTokenExpiresAt { get; set; }

        [JsonPropertyName("refreshTokenExpiresAt")]
        public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
