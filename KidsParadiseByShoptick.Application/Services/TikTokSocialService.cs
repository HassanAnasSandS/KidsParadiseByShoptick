using System.Net.Http.Json;
using System.Text.Json;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class TikTokSocialService : ITikTokSocialService
{
    private const string ContentInitUrl = "https://open.tiktokapis.com/v2/post/publish/content/init/";

    private readonly TikTokSocialOptions _options;
    private readonly ITikTokAuthService _auth;
    private readonly ISocialMediaSettingsService _socialSettings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly MetaSocialOptions _metaOptions;
    private readonly HttpClient _http;
    private readonly ILogger<TikTokSocialService> _logger;

    public TikTokSocialService(
        IOptions<TikTokSocialOptions> options,
        IOptions<MetaSocialOptions> metaOptions,
        ITikTokAuthService auth,
        ISocialMediaSettingsService socialSettings,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage,
        HttpClient http,
        ILogger<TikTokSocialService> logger)
    {
        _options = options.Value;
        _metaOptions = metaOptions.Value;
        _auth = auth;
        _socialSettings = socialSettings;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _http = http;
        _logger = logger;
    }

    public bool IsOAuthConfigured => _auth.IsOAuthConfigured;

    public bool IsConfigured => _auth.IsOAuthConfigured && _auth.IsConnected;

    public async Task<string?> PostToyPhotosAsync(int toyId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return null;

        var toy = await _unitOfWork.Toys.GetWithDetailsAsync(toyId, cancellationToken);
        if (toy is null)
            throw new InvalidOperationException("Toy not found for TikTok posting.");

        var siteBase = string.IsNullOrWhiteSpace(_metaOptions.SiteBaseUrl)
            ? "https://kidsparadise.shoptick.shop"
            : _metaOptions.SiteBaseUrl;

        var imageUrls = ToySocialCaptionBuilder.BuildAbsoluteImageUrls(toy, siteBase, _fileStorage.GetPublicUrl);
        if (imageUrls.Count == 0)
            throw new InvalidOperationException("TikTok photo post requires at least one image.");

        var settings = await _socialSettings.GetAsync(cancellationToken);
        var caption = ToySocialCaptionBuilder.Build(toy, siteBase, _metaOptions.WhatsAppNumber, settings.Tags);
        var title = Truncate(toy.Name.Trim(), 90);

        var (accessToken, _) = await _auth.GetAccessTokenAsync(cancellationToken);
        var postMode = string.Equals(_options.PostMode, "DIRECT_POST", StringComparison.OrdinalIgnoreCase)
            ? "DIRECT_POST"
            : "MEDIA_UPLOAD";

        var postInfo = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["description"] = Truncate(caption, 4000),
        };

        if (postMode == "DIRECT_POST")
        {
            postInfo["privacy_level"] = string.IsNullOrWhiteSpace(_options.PrivacyLevel)
                ? "SELF_ONLY"
                : _options.PrivacyLevel;
            postInfo["disable_comment"] = false;
            postInfo["auto_add_music"] = true;
        }

        var payload = new
        {
            post_info = postInfo,
            source_info = new
            {
                source = "PULL_FROM_URL",
                photo_cover_index = 0,
                photo_images = imageUrls.Take(35).ToArray(),
            },
            post_mode = postMode,
            media_type = "PHOTO",
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ContentInitUrl);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Content = JsonContent.Create(payload);

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ParseTikTokError(body));

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("error", out var error)
            && error.TryGetProperty("code", out var codeEl)
            && !string.Equals(codeEl.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(ParseTikTokError(body));
        }

        var publishId = doc.RootElement.TryGetProperty("data", out var data)
            && data.TryGetProperty("publish_id", out var pid)
            ? pid.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(publishId))
            throw new InvalidOperationException($"TikTok photo init returned no publish_id: {body}");

        _logger.LogInformation("TikTok photo post initiated for toy {ToyId}: {PublishId}", toyId, publishId);
        return publishId;
    }

    static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    static string ParseTikTokError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var code = error.TryGetProperty("code", out var c) ? c.GetString() : null;
                var message = error.TryGetProperty("message", out var m) ? m.GetString() : null;
                if (!string.IsNullOrWhiteSpace(message))
                    return string.IsNullOrWhiteSpace(code) ? message : $"{code}: {message}";
            }
        }
        catch
        {
            // ignored
        }

        return string.IsNullOrWhiteSpace(body) ? "TikTok API error." : body;
    }
}
