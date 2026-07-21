using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class PinterestSocialService : IPinterestSocialService
{
    private const string ApiBase = "https://api.pinterest.com/v5";

    private readonly PinterestSocialOptions _options;
    private readonly MetaSocialOptions _metaOptions;
    private readonly IPinterestAuthService _auth;
    private readonly ISocialMediaSettingsService _socialSettings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly HttpClient _http;
    private readonly ILogger<PinterestSocialService> _logger;

    public PinterestSocialService(
        IOptions<PinterestSocialOptions> options,
        IOptions<MetaSocialOptions> metaOptions,
        IPinterestAuthService auth,
        ISocialMediaSettingsService socialSettings,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage,
        HttpClient http,
        ILogger<PinterestSocialService> logger)
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

    public async Task<string?> PostToyPinAsync(int toyId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return null;

        var toy = await _unitOfWork.Toys.GetWithDetailsAsync(toyId, cancellationToken);
        if (toy is null)
            throw new InvalidOperationException("Toy not found for Pinterest posting.");

        var siteBase = string.IsNullOrWhiteSpace(_metaOptions.SiteBaseUrl)
            ? "https://kidsparadise.shoptick.shop"
            : _metaOptions.SiteBaseUrl.TrimEnd('/');

        var imageUrls = ToySocialCaptionBuilder.BuildAbsoluteImageUrls(toy, siteBase, _fileStorage.GetPublicUrl);
        if (imageUrls.Count == 0)
            throw new InvalidOperationException("Pinterest pin requires at least one image.");

        var settings = await _socialSettings.GetAsync(cancellationToken);
        var caption = ToySocialCaptionBuilder.Build(toy, siteBase, _metaOptions.WhatsAppNumber, settings.Tags);
        var description = Truncate(StripHtml(caption), 800);
        var title = Truncate($"{toy.Name.Trim()} – Kids Toys Pakistan", 100);
        var link = $"{siteBase}/product/{toy.Id}";
        var altText = Truncate($"Buy {toy.Name.Trim()} online in Pakistan – Kids Paradise by Shoptick", 500);

        var accessToken = await _auth.GetAccessTokenAsync(cancellationToken);
        var boardId = await EnsureBoardIdAsync(accessToken, cancellationToken);

        var payload = new Dictionary<string, object?>
        {
            ["board_id"] = boardId,
            ["title"] = title,
            ["description"] = description,
            ["link"] = link,
            ["alt_text"] = altText,
            ["media_source"] = new Dictionary<string, object?>
            {
                ["source_type"] = "image_url",
                ["url"] = imageUrls[0],
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/pins");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(payload);

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ParsePinterestError(body));

        using var doc = JsonDocument.Parse(body);
        var pinId = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(pinId))
            throw new InvalidOperationException($"Pinterest pin create returned no id: {body}");

        _logger.LogInformation("Pinterest pin created for toy {ToyId}: {PinId}", toyId, pinId);
        return pinId;
    }

    async Task<string> EnsureBoardIdAsync(string accessToken, CancellationToken cancellationToken)
    {
        var existing = await _auth.GetSavedBoardIdAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        var boardName = string.IsNullOrWhiteSpace(_options.DefaultBoardName)
            ? "Kids Paradise Toys"
            : _options.DefaultBoardName.Trim();

        var listed = await FindBoardByNameAsync(accessToken, boardName, cancellationToken);
        if (!string.IsNullOrWhiteSpace(listed))
        {
            await _auth.SaveBoardIdAsync(listed, cancellationToken);
            return listed;
        }

        var created = await CreateBoardAsync(accessToken, boardName, cancellationToken);
        await _auth.SaveBoardIdAsync(created, cancellationToken);
        return created;
    }

    async Task<string?> FindBoardByNameAsync(string accessToken, string boardName, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/boards?page_size=100");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ParsePinterestError(body));

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in items.EnumerateArray())
        {
            var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
            var id = item.TryGetProperty("id", out var i) ? i.GetString() : null;
            if (!string.IsNullOrWhiteSpace(id)
                && string.Equals(name, boardName, StringComparison.OrdinalIgnoreCase))
            {
                return id;
            }
        }

        // Fallback: first writable board if any exist.
        foreach (var item in items.EnumerateArray())
        {
            var id = item.TryGetProperty("id", out var i) ? i.GetString() : null;
            if (!string.IsNullOrWhiteSpace(id))
                return id;
        }

        return null;
    }

    async Task<string> CreateBoardAsync(string accessToken, string boardName, CancellationToken cancellationToken)
    {
        var payload = new
        {
            name = Truncate(boardName, 50),
            description = "Unique kids toys from Kids Paradise by Shoptick — Karachi & Pakistan, cash on delivery.",
            privacy = "PUBLIC",
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/boards");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(payload);

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ParsePinterestError(body));

        using var doc = JsonDocument.Parse(body);
        var id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException($"Pinterest board create returned no id: {body}");

        return id;
    }

    static string ParsePinterestError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString() ?? body;
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                if (err.ValueKind == JsonValueKind.String)
                    return err.GetString() ?? body;
                if (err.TryGetProperty("message", out var em))
                    return em.GetString() ?? body;
            }
        }
        catch
        {
            // fall through
        }

        return string.IsNullOrWhiteSpace(body) ? "Pinterest API error." : body;
    }

    static string StripHtml(string text)
        => Regex.Replace(text ?? string.Empty, "<[^>]+>", " ").Replace("&nbsp;", " ").Trim();

    static string Truncate(string text, int max)
    {
        var clean = Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
        if (clean.Length <= max) return clean;
        return clean[..(max - 1)].TrimEnd() + "…";
    }
}
