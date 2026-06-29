using System.Net.Http.Json;
using System.Text.Json;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly HttpClient _http;
    private readonly ILogger<MetaSocialMediaService> _logger;

    public MetaSocialMediaService(
        IOptions<MetaSocialOptions> options,
        IMetaTokenService metaToken,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage,
        HttpClient http,
        ILogger<MetaSocialMediaService> logger)
    {
        _options = options.Value;
        _metaToken = metaToken;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _http = http;
        _logger = logger;
    }

    public async Task<SocialPostResultDto> PostToyAsync(int toyId, CancellationToken cancellationToken = default)
    {
        if (!_metaToken.IsConfigured)
        {
            return new SocialPostResultDto(false, null, false, null,
                "Facebook/Instagram posting is not configured on the server.");
        }

        MetaPageCredentials credentials;
        try
        {
            credentials = await _metaToken.EnsureCredentialsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meta credentials unavailable for toy {ToyId}", toyId);
            return new SocialPostResultDto(false, null, false, null, ex.Message);
        }

        var toy = await _unitOfWork.Toys.GetWithDetailsAsync(toyId, cancellationToken);
        if (toy is null)
            return new SocialPostResultDto(false, null, false, null, "Toy not found for social posting.");

        var caption = ToySocialCaptionBuilder.Build(toy, _options.SiteBaseUrl, _options.WhatsAppNumber);
        var imageUrls = ToySocialCaptionBuilder.BuildAbsoluteImageUrls(
            toy, _options.SiteBaseUrl, _fileStorage.GetPublicUrl);

        string? facebookPostId = null;
        string? instagramPostId = null;
        var messages = new List<string>();

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

        var summary = messages.Count == 0
            ? BuildSuccessMessage(facebookPostId, instagramPostId)
            : string.Join(" ", messages);

        return new SocialPostResultDto(
            facebookPostId is not null,
            facebookPostId,
            instagramPostId is not null,
            instagramPostId,
            summary);
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

    static string BuildSuccessMessage(string? facebookPostId, string? instagramPostId)
    {
        var parts = new List<string>();
        if (facebookPostId is not null) parts.Add("Facebook posted.");
        if (instagramPostId is not null) parts.Add("Instagram posted.");
        return parts.Count == 0 ? "Nothing posted." : string.Join(" ", parts);
    }
}
