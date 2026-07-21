using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class PinterestAuthService : IPinterestAuthService
{
    private const string AuthUri = "https://www.pinterest.com/oauth/";
    private const string TokenUri = "https://api.pinterest.com/v5/oauth/token";
    private const string CacheKeyPrefix = "pinterest-oauth-state:";

    private readonly PinterestSocialOptions _options;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _http;
    private readonly ILogger<PinterestAuthService> _logger;
    private readonly string _tokenFilePath;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public PinterestAuthService(
        IOptions<PinterestSocialOptions> options,
        IMemoryCache cache,
        IConfiguration configuration,
        HttpClient http,
        ILogger<PinterestAuthService> logger)
    {
        _options = options.Value;
        _cache = cache;
        _http = http;
        _logger = logger;

        var basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "KidsParadiseByShoptick.Published");
        basePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(basePath);
        _tokenFilePath = Path.Combine(basePath, ".app-data", "pinterest-oauth.json");
    }

    public bool IsOAuthConfigured =>
        _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.AppId)
        && !string.IsNullOrWhiteSpace(_options.AppSecret);

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
            throw new InvalidOperationException("PinterestSocial:RedirectUri is not configured.");

        state = Guid.NewGuid().ToString("N");
        _cache.Set(CacheKeyPrefix + state, true, TimeSpan.FromMinutes(15));

        return AuthUri +
               $"?client_id={Uri.EscapeDataString(_options.AppId)}" +
               $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
               "&response_type=code" +
               $"&scope={Uri.EscapeDataString(_options.Scopes)}" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task CompleteAuthorizationAsync(string state, string code, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKeyPrefix + state, out bool _))
            _cache.Remove(CacheKeyPrefix + state);
        else if (string.IsNullOrWhiteSpace(state))
            throw new InvalidOperationException("OAuth state missing. Start again from Social Settings.");

        EnsureOAuthConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUri);
        request.Headers.Authorization = BuildBasicAuth();
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
        });

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Pinterest token exchange failed: {body}");

        await SaveTokenResponseAsync(body, preserveRefreshIfMissing: false, preserveBoardId: true, cancellationToken);
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            var store = LoadStore();
            if (!string.IsNullOrWhiteSpace(store.AccessToken)
                && store.AccessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                return store.AccessToken;
            }

            if (string.IsNullOrWhiteSpace(store.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Pinterest is not connected. Open Social Settings → Connect Pinterest.");
            }

            await RefreshLockedAsync(store, cancellationToken);
            store = LoadStore();
            if (string.IsNullOrWhiteSpace(store.AccessToken))
                throw new InvalidOperationException("Pinterest token refresh did not return access token.");

            return store.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public Task<string?> GetSavedBoardIdAsync(CancellationToken cancellationToken = default)
    {
        var configured = _options.BoardId?.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
            return Task.FromResult<string?>(configured);

        var store = LoadStore();
        return Task.FromResult(string.IsNullOrWhiteSpace(store.BoardId) ? null : store.BoardId);
    }

    public async Task SaveBoardIdAsync(string boardId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(boardId))
            throw new ArgumentException("Board id is required.", nameof(boardId));

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            var store = LoadStore();
            store.BoardId = boardId.Trim();
            store.UpdatedAt = DateTimeOffset.UtcNow;
            await WriteStoreAsync(store, cancellationToken);
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
            _logger.LogWarning(ex, "Pinterest token refresh failed.");
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
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUri);
        request.Headers.Authorization = BuildBasicAuth();
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = store.RefreshToken!,
        });

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Pinterest refresh failed: {body}");

        await SaveTokenResponseAsync(body, preserveRefreshIfMissing: true, preserveBoardId: true, cancellationToken);
    }

    async Task SaveTokenResponseAsync(
        string body, bool preserveRefreshIfMissing, bool preserveBoardId, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 2592000;
        var refreshExpiresIn = root.TryGetProperty("refresh_token_expires_in", out var rexp) ? rexp.GetInt32() : 0;

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException($"Pinterest token response missing access_token: {body}");

        var existing = LoadStore();
        if (string.IsNullOrWhiteSpace(refreshToken) && preserveRefreshIfMissing)
            refreshToken = existing.RefreshToken;

        var store = new TokenStore
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60)),
            RefreshTokenExpiresAt = refreshExpiresIn > 0
                ? DateTimeOffset.UtcNow.AddSeconds(refreshExpiresIn)
                : existing.RefreshTokenExpiresAt,
            BoardId = preserveBoardId ? existing.BoardId : null,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        if (!string.IsNullOrWhiteSpace(_options.BoardId))
            store.BoardId = _options.BoardId.Trim();

        await WriteStoreAsync(store, cancellationToken);
    }

    async Task WriteStoreAsync(TokenStore store, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tokenFilePath)!);
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

    AuthenticationHeaderValue BuildBasicAuth()
    {
        var raw = $"{_options.AppId}:{_options.AppSecret}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
    }

    void EnsureOAuthConfigured()
    {
        if (!IsOAuthConfigured)
        {
            throw new InvalidOperationException(
                "Pinterest is not configured. Set PinterestSocial:Enabled, AppId, AppSecret in appsettings/Secrets.");
        }
    }

    private sealed class TokenStore
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("accessTokenExpiresAt")]
        public DateTimeOffset AccessTokenExpiresAt { get; set; }

        [JsonPropertyName("refreshTokenExpiresAt")]
        public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

        [JsonPropertyName("boardId")]
        public string? BoardId { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
