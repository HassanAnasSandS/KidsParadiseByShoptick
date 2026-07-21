using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using KidsParadiseByShoptick.Application.Helpers;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class MetaSocialMediaService : ISocialMediaService
{
    private const string GraphBase = "https://graph.facebook.com/v21.0";

    private readonly MetaSocialOptions _options;
    private readonly IMetaTokenService _metaToken;
    private readonly ISocialMediaSettingsService _socialSettings;
    private readonly ITikTokSocialService _tikTok;
    private readonly IPinterestSocialService _pinterest;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly HttpClient _http;
    private readonly ILogger<MetaSocialMediaService> _logger;

    public MetaSocialMediaService(
        IOptions<MetaSocialOptions> options,
        IMetaTokenService metaToken,
        ISocialMediaSettingsService socialSettings,
        ITikTokSocialService tikTok,
        IPinterestSocialService pinterest,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage,
        HttpClient http,
        ILogger<MetaSocialMediaService> logger)
    {
        _options = options.Value;
        _metaToken = metaToken;
        _socialSettings = socialSettings;
        _tikTok = tikTok;
        _pinterest = pinterest;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _http = http;
        _logger = logger;
    }

    public async Task<SocialPostResultDto> PostToyAsync(int toyId, CancellationToken cancellationToken = default)
    {
        var toy = await _unitOfWork.Toys.GetWithDetailsAsync(toyId, cancellationToken);
        if (toy is null)
            return new SocialPostResultDto(false, null, false, null, "Toy not found for social posting.");

        var settings = await _socialSettings.GetAsync(cancellationToken);
        var caption = ToySocialCaptionBuilder.Build(toy, _options.SiteBaseUrl, _options.WhatsAppNumber, settings.Tags);
        var imageUrls = ToySocialCaptionBuilder.BuildAbsoluteImageUrls(
            toy, _options.SiteBaseUrl, _fileStorage.GetPublicUrl);

        string? facebookPostId = null;
        string? instagramPostId = null;
        string? whatsAppCatalogProductId = null;
        string? tikTokPublishId = null;
        string? pinterestPinId = null;
        var messages = new List<string>();

        MetaPageCredentials? credentials = null;
        if (!_metaToken.IsConfigured)
        {
            messages.Add("Facebook/Instagram/WhatsApp: not configured on the server.");
        }
        else
        {
            try
            {
                credentials = await _metaToken.EnsureCredentialsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Meta credentials unavailable for toy {ToyId}", toyId);
                messages.Add($"Facebook/Instagram/WhatsApp: {ex.Message}");
            }
        }

        if (credentials is not null)
        {
            try
            {
                facebookPostId = await PostToFacebookAsync(credentials, caption, imageUrls, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Facebook post failed for toy {ToyId}", toyId);
                messages.Add($"Facebook: {ex.Message}");
            }

            var igId = credentials.InstagramBusinessAccountId;
            if (!string.IsNullOrWhiteSpace(igId))
            {
                if (imageUrls.Count == 0)
                {
                    messages.Add("Instagram: skipped (at least one photo is required).");
                }
                else
                {
                    try
                    {
                        instagramPostId = await PostToInstagramAsync(credentials, igId, caption, imageUrls, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Instagram post failed for toy {ToyId}", toyId);
                        messages.Add($"Instagram: {ex.Message}");
                    }
                }
            }

            var catalogId = FirstNonEmpty(_options.WhatsAppCatalogId, credentials.WhatsAppCatalogId);
            if (!_options.WhatsAppCatalogEnabled)
            {
                // Temporarily disabled — re-enable via MetaSocial:WhatsAppCatalogEnabled.
            }
            else if (string.IsNullOrWhiteSpace(catalogId))
            {
                messages.Add("Meta catalog: skipped (WhatsAppCatalogId not configured).");
            }
            else if (imageUrls.Count == 0)
            {
                messages.Add("Meta catalog: skipped (at least one photo is required).");
            }
            else
            {
                try
                {
                    whatsAppCatalogProductId = await UpsertWhatsAppCatalogProductAsync(
                        catalogId, credentials.PageAccessToken, toy, caption, imageUrls, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Meta catalog upsert failed for toy {ToyId}", toyId);
                    messages.Add($"Meta catalog: {ex.Message}");
                }
            }
        }

        // TikTok runs independently of Meta (same Create/Update social queue).
        if (!_tikTok.IsOAuthConfigured)
        {
            messages.Add("TikTok: skipped (not enabled/configured on the server).");
        }
        else if (!_tikTok.IsConfigured)
        {
            messages.Add("TikTok: skipped (not connected — open Social Settings → Connect TikTok).");
        }
        else if (imageUrls.Count == 0)
        {
            messages.Add("TikTok: skipped (at least one photo is required).");
        }
        else
        {
            try
            {
                tikTokPublishId = await _tikTok.PostToyPhotosAsync(toyId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TikTok photo post failed for toy {ToyId}", toyId);
                messages.Add($"TikTok: {ex.Message}");
            }
        }

        // Pinterest runs independently (same Create/Update social queue).
        if (!_pinterest.IsOAuthConfigured)
        {
            messages.Add("Pinterest: skipped (not enabled/configured on the server).");
        }
        else if (!_pinterest.IsConfigured)
        {
            messages.Add("Pinterest: skipped (not connected — open Social Settings → Connect Pinterest).");
        }
        else if (imageUrls.Count == 0)
        {
            messages.Add("Pinterest: skipped (at least one photo is required).");
        }
        else
        {
            try
            {
                pinterestPinId = await _pinterest.PostToyPinAsync(toyId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pinterest pin failed for toy {ToyId}", toyId);
                messages.Add($"Pinterest: {ex.Message}");
            }
        }

        var anySuccess = facebookPostId is not null
            || instagramPostId is not null
            || whatsAppCatalogProductId is not null
            || tikTokPublishId is not null
            || pinterestPinId is not null;

        var summary = messages.Count == 0
            ? BuildSuccessMessage(facebookPostId, instagramPostId, whatsAppCatalogProductId, tikTokPublishId, pinterestPinId)
            : anySuccess
                ? string.Join(" ", new[] { BuildSuccessMessage(facebookPostId, instagramPostId, whatsAppCatalogProductId, tikTokPublishId, pinterestPinId) }.Concat(messages).Where(s => !string.IsNullOrWhiteSpace(s)))
                : string.Join(" ", messages);

        return new SocialPostResultDto(
            facebookPostId is not null,
            facebookPostId,
            instagramPostId is not null,
            instagramPostId,
            summary,
            Queued: false,
            whatsAppCatalogProductId is not null,
            whatsAppCatalogProductId,
            tikTokPublishId is not null,
            tikTokPublishId,
            pinterestPinId is not null,
            pinterestPinId);
    }

    async Task<string> UpsertWhatsAppCatalogProductAsync(
        string catalogId,
        string accessToken,
        Domain.Entities.Toy toy,
        string description,
        IReadOnlyList<string> imageUrls,
        CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>
        {
            ["id"] = toy.Id.ToString(CultureInfo.InvariantCulture),
            ["title"] = Truncate(toy.Name.Trim(), 100),
            ["description"] = Truncate(StripHtml(description), 5000),
            ["availability"] = toy.IsSold ? "out of stock" : "in stock",
            ["condition"] = "used",
            ["link"] = $"{_options.SiteBaseUrl.TrimEnd('/')}/product/{toy.Id}",
            ["image_link"] = imageUrls[0],
            ["brand"] = "Kids Paradise",
            ["price"] = FormatCatalogPrice(toy),
        };

        if (toy.SalePrice is not null && toy.SalePrice < toy.Price)
            data["sale_price"] = FormatCatalogSalePrice(toy);

        if (imageUrls.Count > 1)
            data["additional_image_link"] = imageUrls.Skip(1).Take(9).ToArray();

        var requestsJson = JsonSerializer.Serialize(new[]
        {
            new { method = "UPDATE", data },
        });

        var url = $"{GraphBase}/{catalogId}/items_batch";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["item_type"] = "PRODUCT_ITEM",
            ["item_sub_type"] = "TOYS",
            ["allow_upsert"] = "true",
            ["requests"] = requestsJson,
            ["access_token"] = accessToken,
        });

        using var response = await _http.PostAsync(url, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ParseGraphError(body));

        EnsureCatalogBatchSuccess(body);
        return toy.Id.ToString(CultureInfo.InvariantCulture);
    }

    static void EnsureCatalogBatchSuccess(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("validation_status", out var statuses))
            return;

        var errors = new List<string>();
        foreach (var status in statuses.EnumerateArray())
        {
            if (!status.TryGetProperty("errors", out var errorItems))
                continue;

            foreach (var error in errorItems.EnumerateArray())
            {
                if (error.TryGetProperty("message", out var messageEl))
                {
                    var message = messageEl.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                        errors.Add(message);
                }
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", errors.Distinct()));
    }

    static string FormatCatalogPrice(Domain.Entities.Toy toy)
    {
        var amount = toy.Price.ToString("0", CultureInfo.InvariantCulture);
        return $"{amount} PKR";
    }

    static string FormatCatalogSalePrice(Domain.Entities.Toy toy)
    {
        var amount = toy.SalePrice!.Value.ToString("0", CultureInfo.InvariantCulture);
        return $"{amount} PKR";
    }

    static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    static string StripHtml(string value) =>
        value
            .Replace("<del>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("</del>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<strong>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("</strong>", string.Empty, StringComparison.OrdinalIgnoreCase);

    static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    async Task<string?> PostToFacebookAsync(
        MetaPageCredentials credentials, string caption, IReadOnlyList<string> imageUrls, CancellationToken cancellationToken)
    {
        var pageId = credentials.FacebookPageId;
        var token = credentials.PageAccessToken;

        if (imageUrls.Count == 0)
        {
            var link = ExtractProductLink(caption);
            return await PostFacebookFeedAsync(pageId, token, caption, link, cancellationToken);
        }

        if (imageUrls.Count == 1)
            return await PostFacebookSinglePhotoAsync(pageId, token, caption, imageUrls[0], cancellationToken);

        var mediaIds = new List<string>();
        foreach (var url in imageUrls.Take(10))
        {
            var photoId = await UploadFacebookUnpublishedPhotoAsync(pageId, token, url, cancellationToken);
            mediaIds.Add(photoId);
        }

        return await PostFacebookFeedWithPhotosAsync(pageId, token, caption, mediaIds, cancellationToken);
    }

    async Task<string?> PostToInstagramAsync(
        MetaPageCredentials credentials, string igId, string caption, IReadOnlyList<string> imageUrls, CancellationToken cancellationToken)
    {
        var token = credentials.PageAccessToken;
        // Instagram does not render HTML; plain text matches app posts and keeps Promote cleaner.
        caption = StripHtml(caption);

        if (imageUrls.Count == 1)
        {
            var creationId = await CreateInstagramMediaAsync(igId, token, imageUrls[0], caption, cancellationToken);
            return await PublishInstagramMediaAsync(igId, token, creationId, cancellationToken);
        }

        var childIds = new List<string>();
        foreach (var url in imageUrls.Take(10))
        {
            var childId = await CreateInstagramCarouselItemAsync(igId, token, url, cancellationToken);
            childIds.Add(childId);
        }

        var carouselId = await CreateInstagramCarouselAsync(igId, token, caption, childIds, cancellationToken);
        return await PublishInstagramMediaAsync(igId, token, carouselId, cancellationToken);
    }

    async Task<string> PostFacebookSinglePhotoAsync(
        string pageId, string token, string caption, string imageUrl, CancellationToken cancellationToken)
    {
        var url = $"{GraphBase}/{pageId}/photos";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["url"] = imageUrl,
            ["caption"] = caption,
            ["access_token"] = token,
        });

        using var response = await _http.PostAsync(url, content, cancellationToken);
        return await ReadGraphIdAsync(response, cancellationToken);
    }

    async Task<string> UploadFacebookUnpublishedPhotoAsync(
        string pageId, string token, string imageUrl, CancellationToken cancellationToken)
    {
        var url = $"{GraphBase}/{pageId}/photos";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["url"] = imageUrl,
            ["published"] = "false",
            ["access_token"] = token,
        });

        using var response = await _http.PostAsync(url, content, cancellationToken);
        return await ReadGraphIdAsync(response, cancellationToken);
    }

    async Task<string> PostFacebookFeedWithPhotosAsync(
        string pageId, string token, string caption, IReadOnlyList<string> photoIds, CancellationToken cancellationToken)
    {
        var url = $"{GraphBase}/{pageId}/feed";
        var fields = new Dictionary<string, string>
        {
            ["message"] = caption,
            ["access_token"] = token,
        };

        for (var i = 0; i < photoIds.Count; i++)
            fields[$"attached_media[{i}]"] = JsonSerializer.Serialize(new { media_fbid = photoIds[i] });

        using var content = new FormUrlEncodedContent(fields);
        using var response = await _http.PostAsync(url, content, cancellationToken);
        return await ReadGraphIdAsync(response, cancellationToken);
    }

    async Task<string> PostFacebookFeedAsync(
        string pageId, string token, string caption, string? link, CancellationToken cancellationToken)
    {
        var url = $"{GraphBase}/{pageId}/feed";
        var fields = new Dictionary<string, string>
        {
            ["message"] = caption,
            ["access_token"] = token,
        };
        if (!string.IsNullOrWhiteSpace(link))
            fields["link"] = link;

        using var content = new FormUrlEncodedContent(fields);
        using var response = await _http.PostAsync(url, content, cancellationToken);
        return await ReadGraphIdAsync(response, cancellationToken);
    }

    async Task<string> CreateInstagramMediaAsync(
        string igId, string token, string imageUrl, string caption, CancellationToken cancellationToken)
    {
        var url = $"{GraphBase}/{igId}/media";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["image_url"] = imageUrl,
            ["caption"] = caption,
            ["access_token"] = token,
        });

        using var response = await _http.PostAsync(url, content, cancellationToken);
        return await ReadGraphIdAsync(response, cancellationToken);
    }

    async Task<string> CreateInstagramCarouselItemAsync(
        string igId, string token, string imageUrl, CancellationToken cancellationToken)
    {
        var url = $"{GraphBase}/{igId}/media";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["image_url"] = imageUrl,
            ["is_carousel_item"] = "true",
            ["access_token"] = token,
        });

        using var response = await _http.PostAsync(url, content, cancellationToken);
        return await ReadGraphIdAsync(response, cancellationToken);
    }

    async Task<string> CreateInstagramCarouselAsync(
        string igId, string token, string caption, IReadOnlyList<string> childIds, CancellationToken cancellationToken)
    {
        var url = $"{GraphBase}/{igId}/media";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["media_type"] = "CAROUSEL",
            ["caption"] = caption,
            ["children"] = string.Join(",", childIds),
            ["access_token"] = token,
        });

        using var response = await _http.PostAsync(url, content, cancellationToken);
        return await ReadGraphIdAsync(response, cancellationToken);
    }

    async Task<string> PublishInstagramMediaAsync(
        string igId, string token, string creationId, CancellationToken cancellationToken)
    {
        var url = $"{GraphBase}/{igId}/media_publish";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["creation_id"] = creationId,
            ["access_token"] = token,
        });

        using var response = await _http.PostAsync(url, content, cancellationToken);
        return await ReadGraphIdAsync(response, cancellationToken);
    }

    static async Task<string> ReadGraphIdAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ParseGraphError(body));

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("id", out var idEl))
            return idEl.GetString() ?? throw new InvalidOperationException("Graph API did not return an id.");

        throw new InvalidOperationException($"Unexpected Graph API response: {body}");
    }

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

    static string? ExtractProductLink(string caption)
    {
        foreach (var line in caption.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("🛒 ", StringComparison.Ordinal))
                return trimmed[2..].Trim();
        }

        return null;
    }

    static string BuildSuccessMessage(
        string? facebookPostId,
        string? instagramPostId,
        string? whatsAppCatalogProductId,
        string? tikTokPublishId,
        string? pinterestPinId)
    {
        var parts = new List<string>();
        if (facebookPostId is not null) parts.Add("Facebook posted.");
        if (instagramPostId is not null) parts.Add("Instagram posted.");
        if (whatsAppCatalogProductId is not null) parts.Add("Meta catalog updated.");
        if (tikTokPublishId is not null) parts.Add("TikTok photo post started.");
        if (pinterestPinId is not null) parts.Add("Pinterest pin created.");
        return parts.Count == 0 ? "Nothing posted." : string.Join(" ", parts);
    }
}
