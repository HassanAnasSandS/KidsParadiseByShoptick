using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class YouTubeAuthService : IYouTubeAuthService
{
    private const string AuthUri = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenUri = "https://oauth2.googleapis.com/token";
    private const string CacheKeyPrefix = "youtube-oauth-state:";

    private readonly GoogleOAuthOptions _options;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _http;
    private readonly string _tokenFilePath;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public YouTubeAuthService(
        IOptions<GoogleOAuthOptions> options,
        IMemoryCache cache,
        IConfiguration configuration,
        HttpClient http)
    {
        _options = options.Value;
        _cache = cache;
        _http = http;

        var basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "KidsParadiseByShoptick.Published");
        basePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(basePath);
        _tokenFilePath = Path.Combine(basePath, ".app-data", "youtube-oauth.json");
    }

    public bool IsConnected => File.Exists(_tokenFilePath) && !string.IsNullOrWhiteSpace(LoadRefreshToken());

    public string BuildAuthorizationUrl(out string state)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
            throw new InvalidOperationException("Google OAuth ClientId is not configured.");
        if (string.IsNullOrWhiteSpace(_options.RedirectUri))
            throw new InvalidOperationException("Google OAuth RedirectUri is not configured.");

        state = Guid.NewGuid().ToString("N");
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        _cache.Set(
            CacheKeyPrefix + state,
            codeVerifier,
            TimeSpan.FromMinutes(15));

        return $"{AuthUri}?" +
               $"client_id={Uri.EscapeDataString(_options.ClientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
               "&response_type=code" +
               $"&scope={Uri.EscapeDataString(_options.YouTubeUploadScope)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
               "&code_challenge_method=S256" +
               "&access_type=offline" +
               "&prompt=consent";
    }

    public async Task CompleteAuthorizationAsync(
        string state, string code, CancellationToken cancellationToken = default)
    {
        if (!_cache.TryGetValue(CacheKeyPrefix + state, out string? codeVerifier) || string.IsNullOrWhiteSpace(codeVerifier))
            throw new InvalidOperationException("OAuth state expired or invalid. Start again from the admin app.");

        _cache.Remove(CacheKeyPrefix + state);

        using var form = new FormUrlEncodedContent(BuildTokenForm(new Dictionary<string, string>
        {
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["redirect_uri"] = _options.RedirectUri,
            ["grant_type"] = "authorization_code",
        }));

        using var response = await _http.PostAsync(TokenUri, form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google token exchange failed: {body}");

        using var doc = JsonDocument.Parse(body);
        var refreshToken = doc.RootElement.TryGetProperty("refresh_token", out var refreshEl)
            ? refreshEl.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException("Google did not return a refresh token. Remove app access in Google Account and try again.");

        await SaveRefreshTokenAsync(refreshToken, cancellationToken);
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var refreshToken = LoadRefreshToken();
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException("YouTube is not connected. Authorize from the admin app first.");

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            using var form = new FormUrlEncodedContent(BuildTokenForm(new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token",
            }));

            using var response = await _http.PostAsync(TokenUri, form, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Google refresh failed: {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("Google did not return an access token.");
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    string? LoadRefreshToken()
    {
        if (!File.Exists(_tokenFilePath))
            return null;

        try
        {
            var json = File.ReadAllText(_tokenFilePath);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("refreshToken", out var tokenEl)
                ? tokenEl.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    async Task SaveRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(_tokenFilePath)!;
        Directory.CreateDirectory(dir);

        var payload = JsonSerializer.Serialize(new
        {
            refreshToken,
            updatedAt = DateTime.UtcNow,
        });

        await File.WriteAllTextAsync(_tokenFilePath, payload, cancellationToken);
    }

    Dictionary<string, string> BuildTokenForm(Dictionary<string, string> fields)
    {
        fields["client_id"] = _options.ClientId;
        if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
            fields["client_secret"] = _options.ClientSecret;
        return fields;
    }

    static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    static string GenerateCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
