using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KidsParadiseByShoptick.Application.Helpers;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class TikTokSocialService : ITikTokSocialService
{
    private const string ContentInitUrl = "https://open.tiktokapis.com/v2/post/publish/content/init/";
    private const string StatusFetchUrl = "https://open.tiktokapis.com/v2/post/publish/status/fetch/";

    private readonly TikTokSocialOptions _options;
    private readonly ITikTokAuthService _auth;
    private readonly ISocialMediaSettingsService _socialSettings;
    private static readonly JsonSerializerOptions TikTokJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly ITikTokPhotoPrepareService _photoPrepare;
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
        ITikTokPhotoPrepareService photoPrepare,
        HttpClient http,
        ILogger<TikTokSocialService> logger)
    {
        _options = options.Value;
        _metaOptions = metaOptions.Value;
        _auth = auth;
        _socialSettings = socialSettings;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _photoPrepare = photoPrepare;
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

        var relativePaths = toy.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => i.ImagePath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Cast<string>()
            .ToList();

        Log($"start toy={toyId} images={relativePaths.Count}");

        // TikTok rejects PNG and anything over 1080p (typical toy photos are 1204×1600).
        var imageUrls = await _photoPrepare.PreparePublicJpegUrlsAsync(toy.Id, relativePaths, siteBase, cancellationToken);
        Log($"prepared toy={toyId} count={imageUrls.Count} first={(imageUrls.Count == 0 ? "-" : imageUrls[0])}");
        if (imageUrls.Count == 0)
            throw new InvalidOperationException("TikTok photo post requires at least one image that can be converted to JPEG 1080p.");

        var settings = await _socialSettings.GetAsync(cancellationToken);
        // TikTok only allows a few hashtags — cap at 5 even if Social Settings has more.
        var tikTokTags = SocialMediaTagHelper.FormatTagsForCaption(
            settings.Tags, SocialMediaTagHelper.TikTokMaxHashtags);
        var caption = ToySocialCaptionBuilder.BuildPlainText(
            toy, siteBase, _metaOptions.WhatsAppNumber, tikTokTags);
        caption = SocialMediaTagHelper.LimitHashtagsInCaption(
            caption, SocialMediaTagHelper.TikTokMaxHashtags);

        var (accessToken, _) = await _auth.GetAccessTokenAsync(cancellationToken);
        var postMode = TikTokSocialOptions.NormalizePostMode(settings.TikTokPostMode);
        var isDirect = postMode == TikTokSocialOptions.DirectPost;
        var title = Truncate(toy.Name.Trim(), 90);
        var description = Truncate(caption, 4000);

        var photos = imageUrls.ToArray();
        var json = isDirect
            ? JsonSerializer.Serialize(new
            {
                post_info = new
                {
                    title,
                    description,
                    privacy_level = await ResolvePrivacyLevelAsync(accessToken, cancellationToken),
                    disable_comment = false,
                    auto_add_music = true,
                    brand_organic_toggle = true,
                },
                source_info = new
                {
                    source = "PULL_FROM_URL",
                    photo_cover_index = 0,
                    photo_images = photos,
                },
                post_mode = postMode,
                media_type = "PHOTO",
            }, TikTokJson)
            : JsonSerializer.Serialize(new
            {
                post_info = new { title, description },
                source_info = new
                {
                    source = "PULL_FROM_URL",
                    photo_cover_index = 0,
                    photo_images = photos,
                },
                post_mode = postMode,
                media_type = "PHOTO",
            }, TikTokJson);

        _logger.LogInformation(
            "TikTok photo init toy {ToyId} mode={Mode} images={Count} first={Url}",
            toy.Id, postMode, imageUrls.Count, imageUrls[0]);

        using var request = new HttpRequestMessage(HttpMethod.Post, ContentInitUrl);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "UTF-8",
        };

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            Log($"init HTTP {(int)response.StatusCode} toy={toyId} {ParseTikTokError(body)}");
            throw new InvalidOperationException(ParseTikTokError(body));
        }

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

        _logger.LogInformation(
            "TikTok photo post initiated for toy {ToyId}: {PublishId} (mode={Mode}, images={Count})",
            toyId, publishId, postMode, imageUrls.Count);

        // Init success ≠ delivered. PULL_FROM_URL often fails later (ownership / pull / size).
        var outcome = await WaitForPublishOutcomeAsync(accessToken, publishId, postMode, cancellationToken);
        Log($"outcome toy={toyId} publishId={publishId} status={outcome}");
        _logger.LogInformation(
            "TikTok photo post outcome for toy {ToyId}: {PublishId} status={Status}",
            toyId, publishId, outcome);

        return publishId;
    }

    /// <summary>
    /// Polls until inbox delivery / publish complete / failed, or times out (still returns — caller already has publish_id).
    /// Throws when TikTok reports FAILED with a reason.
    /// </summary>
    async Task<string> WaitForPublishOutcomeAsync(
        string accessToken,
        string publishId,
        string postMode,
        CancellationToken cancellationToken)
    {
        // Photo pull is usually quick; allow ~4 minutes. TikTok download can lag on several images.
        const int maxAttempts = 48;
        var delay = TimeSpan.FromSeconds(5);
        string? lastStatus = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(delay, cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, StatusFetchUrl);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
            request.Content = JsonContent.Create(new { publish_id = publishId });

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "TikTok status fetch HTTP {Status} for {PublishId}: {Body}",
                    (int)response.StatusCode, publishId, body);
                continue;
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (!doc.RootElement.TryGetProperty("data", out var data))
                continue;

            var status = data.TryGetProperty("status", out var st) ? st.GetString() : null;
            if (string.IsNullOrWhiteSpace(status))
                continue;

            lastStatus = status;

            if (string.Equals(status, "FAILED", StringComparison.OrdinalIgnoreCase))
            {
                var reason = data.TryGetProperty("fail_reason", out var fr) ? fr.GetString() : null;
                throw new InvalidOperationException(FormatFailReason(reason));
            }

            if (IsDeliveredStatus(status))
                return status;

            // PROCESSING_DOWNLOAD / PROCESSING_UPLOAD — keep waiting
            _logger.LogInformation(
                "TikTok publish {PublishId} still {Status} (attempt {Attempt}/{Max})",
                publishId, status, attempt, maxAttempts);
        }

        throw new InvalidOperationException(
            "TikTok could not download the photos" +
            (string.IsNullOrWhiteSpace(lastStatus) ? "" : $" ({lastStatus})") +
            ". Cloudflare is blocking TikTok's servers from fetching /uploads. " +
            "In Cloudflare → Security → Bots, skip Bot Fight / Super Bot Fight when URI Path starts with /uploads/. " +
            "Then retry the toy. Also clear old TikTok inbox drafts (max 5 pending).");
    }

    static bool IsDeliveredStatus(string status)
    {
        // Official: SEND_TO_USER_INBOX / PUBLISH_COMPLETE. Some responses use SENDING_TO_USER_INBOX.
        return status.Contains("INBOX", StringComparison.OrdinalIgnoreCase)
            || status.Contains("PUBLISH_COMPLETE", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "PUBLISHED", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Picks a privacy level allowed for this creator. Prefers configured value, then PUBLIC_TO_EVERYONE.
    /// </summary>
    public async Task<string> ResolvePrivacyLevelAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var preferred = string.IsNullOrWhiteSpace(_options.PrivacyLevel)
            ? "PUBLIC_TO_EVERYONE"
            : _options.PrivacyLevel.Trim();

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://open.tiktokapis.com/v2/post/publish/creator_info/query/");
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
            request.Content = JsonContent.Create(new { });

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TikTok creator_info query failed HTTP {Status}: {Body}", (int)response.StatusCode, body);
                return preferred;
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (!doc.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("privacy_level_options", out var opts)
                || opts.ValueKind != JsonValueKind.Array)
            {
                return preferred;
            }

            var options = opts.EnumerateArray()
                .Select(e => e.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .ToList();

            if (options.Count == 0)
                return preferred;

            if (options.Any(o => string.Equals(o, preferred, StringComparison.OrdinalIgnoreCase)))
                return preferred;

            var pub = options.FirstOrDefault(o =>
                string.Equals(o, "PUBLIC_TO_EVERYONE", StringComparison.OrdinalIgnoreCase));
            if (pub is not null)
                return pub;

            return options[0];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TikTok creator_info privacy resolve failed; using {Privacy}", preferred);
            return preferred;
        }
    }

    static string FormatFailReason(string? reason) => reason switch
    {
        "photo_pull_failed" =>
            "TikTok could not download the toy images (photo_pull_failed). " +
            "Cloudflare Bot Fight often blocks TikTok's download servers. " +
            "Skip Bot Fight for URI Path /uploads/, verify URL property kidsparadise.shoptick.shop, then retry.",
        "picture_size_check_failed" =>
            "TikTok rejected a photo size/format (picture_size_check_failed). Use JPEG/WebP within TikTok photo limits.",
        "url_ownership_unverified" =>
            "TikTok URL ownership not verified for kidsparadise.shoptick.shop. Add and verify the domain/URL prefix in TikTok Developer portal.",
        "spam_risk_too_many_posts" =>
            "TikTok daily API post limit reached for this account. Try again later or post from the TikTok app.",
        "spam_risk_too_many_pending_share" =>
            "Too many pending TikTok inbox shares (max 5). Open TikTok mobile Inbox, finish or clear old shares, then retry.",
        _ when !string.IsNullOrWhiteSpace(reason) => $"TikTok photo post failed: {reason}",
        _ => "TikTok photo post failed with no reason from TikTok.",
    };

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
                    return string.IsNullOrWhiteSpace(code) ? message! : $"{code}: {message}";
                if (!string.IsNullOrWhiteSpace(code) && !string.Equals(code, "ok", StringComparison.OrdinalIgnoreCase))
                    return FormatFailReason(code);
            }
        }
        catch
        {
            // ignored
        }

        return string.IsNullOrWhiteSpace(body) ? "TikTok API error." : body;
    }

    void Log(string message)
    {
        try
        {
            var path = _fileStorage.GetAbsolutePath(".app-data/tiktok-photo.log");
            if (string.IsNullOrWhiteSpace(path))
                return;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }
    }
}
