using System.Text.Json;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class MetaRequirementsService : IMetaRequirementsService
{
    private const string GraphBase = "https://graph.facebook.com/v21.0";

    private static readonly string[] RequiredScopes =
    [
        "pages_manage_posts",
        "instagram_content_publish",
        "catalog_management",
        "whatsapp_business_management",
    ];

    private readonly MetaSocialOptions _options;
    private readonly IMetaTokenService _metaToken;
    private readonly HttpClient _http;
    private readonly ILogger<MetaRequirementsService> _logger;

    public MetaRequirementsService(
        IOptions<MetaSocialOptions> options,
        IMetaTokenService metaToken,
        HttpClient http,
        ILogger<MetaRequirementsService> logger)
    {
        _options = options.Value;
        _metaToken = metaToken;
        _http = http;
        _logger = logger;
    }

    public async Task<MetaRequirementsStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!_metaToken.IsConfigured)
        {
            return BuildResult(null, null, null, NotConfiguredChecks());
        }

        MetaPageCredentials credentials;
        try
        {
            credentials = await _metaToken.EnsureCredentialsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return BuildResult(null, null, null, TokenErrorChecks(ex.Message));
        }

        var wabaId = credentials.WhatsAppBusinessAccountId;
        var catalogId = credentials.WhatsAppCatalogId;

        if (string.IsNullOrWhiteSpace(wabaId))
        {
            wabaId = await FetchWhatsAppBusinessAccountIdAsync(
                credentials.FacebookPageId, credentials.PageAccessToken, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(catalogId) && !string.IsNullOrWhiteSpace(wabaId))
        {
            catalogId = await FetchCatalogIdAsync(wabaId, credentials.PageAccessToken, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(catalogId))
        {
            catalogId = await FetchPageProductCatalogIdAsync(
                credentials.FacebookPageId, credentials.PageAccessToken, cancellationToken);
        }

        var productCount = await FetchCatalogProductCountAsync(
            catalogId, credentials.PageAccessToken, cancellationToken);
        var whatsAppCatalogSynced = !string.IsNullOrWhiteSpace(wabaId)
            && !string.IsNullOrWhiteSpace(catalogId)
            && await IsCatalogLinkedToWabaAsync(
                wabaId, catalogId, credentials.PageAccessToken, cancellationToken);

        var scopes = await GetGrantedScopesAsync(credentials.PageAccessToken, cancellationToken);
        var checks = BuildChecks(wabaId, catalogId, productCount, whatsAppCatalogSynced, scopes);
        return BuildResult(credentials.FacebookPageId, wabaId, catalogId, checks);
    }

    public async Task<MetaRequirementsStatusDto> LinkCatalogAsync(
        string catalogId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(catalogId))
            throw new InvalidOperationException("Catalog ID is required.");

        var credentials = await _metaToken.EnsureCredentialsAsync(cancellationToken);
        var wabaId = credentials.WhatsAppBusinessAccountId;
        if (string.IsNullOrWhiteSpace(wabaId))
        {
            wabaId = await FetchWhatsAppBusinessAccountIdAsync(
                credentials.FacebookPageId, credentials.PageAccessToken, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(wabaId))
        {
            throw new InvalidOperationException(
                "WhatsApp Business Account is not linked to your Facebook Page. Complete Step 1 first.");
        }

        var url = $"{GraphBase}/{wabaId}/product_catalogs";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["catalog_id"] = catalogId.Trim(),
            ["access_token"] = credentials.PageAccessToken,
        });

        using var response = await _http.PostAsync(url, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ParseGraphError(body));

        _logger.LogInformation("Linked catalog {CatalogId} to WABA {WabaId}", catalogId, wabaId);
        return await GetStatusAsync(cancellationToken);
    }

    static IReadOnlyList<MetaRequirementCheckDto> NotConfiguredChecks() =>
    [
        Req(1, "whatsapp-linked", "WhatsApp Business Account (WABA) linked", false,
            "Meta is not connected.",
            "Graph API Explorer → App: KidsParadiseByShoptick → Generate token with whatsapp_business_management → run Scripts/Connect-MetaSocial.ps1 or POST /api/admin/meta/connect"),
        Req(2, "meta-catalog", "Meta Commerce catalog configured", false,
            "Meta is not connected.",
            "Page Settings → Commerce → create catalog, or set MetaSocial:WhatsAppCatalogId in appsettings."),
        Req(3, "whatsapp-catalog-sync", "WhatsApp app catalog synced", false,
            "Meta is not connected.",
            "After WABA is linked, tap Link Catalog in Social Settings or enable catalog in WhatsApp Business app."),
        Req(4, "catalog-permission", "Token has required permissions", false,
            "Meta is not connected.",
            "Graph API Explorer → Add: catalog_management, whatsapp_business_management, pages_manage_posts, instagram_content_publish → regenerate token"),
        Req(5, "toy-images", "Toy has at least 1 image before posting", false,
            "Checked when saving a toy.",
            "In Toy Edit, tap Add Images before Save. WhatsApp catalog requires image_link."),
    ];

    static IReadOnlyList<MetaRequirementCheckDto> TokenErrorChecks(string error) =>
    [
        Req(1, "whatsapp-linked", "WhatsApp Business Account (WABA) linked", false, error,
            "Reconnect Meta with a fresh token from Graph API Explorer."),
        Req(2, "meta-catalog", "Meta Commerce catalog configured", false, "Blocked by token error.",
            "Fix token first, then verify catalog in Commerce Manager."),
        Req(3, "whatsapp-catalog-sync", "WhatsApp app catalog synced", false, "Blocked by token error.",
            "Fix token first, then link catalog to WABA."),
        Req(4, "catalog-permission", "Token has required permissions", false, "Blocked by token error.",
            "Regenerate token with catalog_management and whatsapp_business_management."),
        Req(5, "toy-images", "Toy has at least 1 image before posting", false,
            "Checked when saving a toy.",
            "Add at least one image on the toy edit screen."),
    ];

    IReadOnlyList<MetaRequirementCheckDto> BuildChecks(
        string? wabaId,
        string? catalogId,
        int? productCount,
        bool whatsAppCatalogSynced,
        IReadOnlyList<string> scopes)
    {
        var whatsAppLinked = !string.IsNullOrWhiteSpace(wabaId);
        var metaCatalogReady = !string.IsNullOrWhiteSpace(catalogId);
        var missingScopes = RequiredScopes.Where(s => !scopes.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
        var hasCatalogPermission = missingScopes.Count == 0;

        var metaCatalogStatus = metaCatalogReady
            ? productCount is > 0
                ? $"Catalog ID: {catalogId} — {productCount} product(s) in Meta catalog (API posting works)."
                : $"Catalog ID: {catalogId} — no products yet. Save a toy with images to add items."
            : "No Meta Commerce catalog found for this Facebook Page.";

        var syncStatus = whatsAppCatalogSynced
            ? "Catalog is linked to WhatsApp Business Account — products should appear in WhatsApp app."
            : metaCatalogReady && productCount is > 0
                ? "Products are in Meta catalog but NOT visible in WhatsApp app (+923217175896) until WABA is linked and catalog is synced."
                : "Complete Steps 1–2 first, then link catalog to WhatsApp.";

        return
        [
            Req(1, "whatsapp-linked", "WhatsApp Business Account (WABA) linked", whatsAppLinked,
                whatsAppLinked
                    ? $"Linked — WABA ID: {wabaId}"
                    : "Page shows +923217175896 but WhatsApp Business API account (WABA) is not linked. Facebook/Instagram can work while WhatsApp catalog stays empty.",
                whatsAppLinked
                    ? "Already linked."
                    : "1) developers.facebook.com → KidsParadiseByShoptick → WhatsApp → Getting Started → add phone 0321-7175896\n2) Graph API Explorer → add permission whatsapp_business_management → Generate Token\n3) Run: .\\Scripts\\Connect-MetaSocial.ps1 -UserAccessToken YOUR_TOKEN\n4) Or Meta Business Suite → Settings → WhatsApp accounts → connect number to Kids Paradise Page"),
            Req(2, "meta-catalog", "Meta Commerce catalog configured", metaCatalogReady,
                metaCatalogStatus,
                metaCatalogReady
                    ? "Catalog is ready. Toys added via Admin app go here automatically."
                    : $"Set MetaSocial:WhatsAppCatalogId ({_options.WhatsAppCatalogId}) in appsettings, then reconnect Meta."),
            Req(3, "whatsapp-catalog-sync", "WhatsApp app catalog synced", whatsAppCatalogSynced,
                syncStatus,
                whatsAppCatalogSynced
                    ? "Already synced."
                    : whatsAppLinked
                        ? $"Social Settings → paste Catalog ID {_options.WhatsAppCatalogId} → tap Link Catalog to WhatsApp.\nOr WhatsApp Business app → Settings → Business tools → Catalog → connect Facebook/Meta catalog."
                        : "Complete Step 1 (WABA link + whatsapp_business_management permission) first. Page phone link alone does not sync catalog to WhatsApp app."),
            Req(4, "catalog-permission", "Token has required permissions", hasCatalogPermission,
                hasCatalogPermission
                    ? "All required permissions granted."
                    : $"Missing: {string.Join(", ", missingScopes)}",
                hasCatalogPermission
                    ? "Already granted."
                    : "Graph API Explorer → Meta App → Add Permissions: catalog_management, whatsapp_business_management, pages_manage_posts, instagram_content_publish, pages_show_list → Generate Access Token → reconnect via Connect-MetaSocial.ps1"),
            Req(5, "toy-images", "Toy has at least 1 image before posting", true,
                "Validated automatically when you save a toy.",
                "Before Save on Toy Create/Update, add at least one photo. Catalog API requires image_link."),
        ];
    }

    async Task<string?> FetchWhatsAppBusinessAccountIdAsync(
        string pageId, string pageToken, CancellationToken cancellationToken)
    {
        var url =
            $"{GraphBase}/{pageId}?fields=whatsapp_business_account{{id}},whatsapp_number&access_token={Uri.EscapeDataString(pageToken)}";
        using var response = await _http.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("whatsapp_business_account", out var wabaEl)
            && wabaEl.TryGetProperty("id", out var idEl))
        {
            return idEl.GetString();
        }

        return null;
    }

    async Task<string?> FetchCatalogIdAsync(
        string wabaId, string accessToken, CancellationToken cancellationToken)
    {
        var wabaUrl =
            $"{GraphBase}/{wabaId}?fields=product_catalog{{id}}&access_token={Uri.EscapeDataString(accessToken)}";
        using var wabaResponse = await _http.GetAsync(wabaUrl, cancellationToken);
        var wabaBody = await wabaResponse.Content.ReadAsStringAsync(cancellationToken);
        if (wabaResponse.IsSuccessStatusCode)
        {
            using var wabaDoc = JsonDocument.Parse(wabaBody);
            if (wabaDoc.RootElement.TryGetProperty("product_catalog", out var catalogEl)
                && catalogEl.TryGetProperty("id", out var catalogIdEl))
            {
                return catalogIdEl.GetString();
            }
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
                return idEl.GetString();
        }

        return null;
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
                return idEl.GetString();
        }

        return null;
    }

    async Task<int?> FetchCatalogProductCountAsync(
        string? catalogId, string accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(catalogId))
            return null;

        var url =
            $"{GraphBase}/{catalogId}?fields=product_count&access_token={Uri.EscapeDataString(accessToken)}";
        using var response = await _http.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("product_count", out var countEl)
            && countEl.TryGetInt32(out var count))
        {
            return count;
        }

        return null;
    }

    async Task<bool> IsCatalogLinkedToWabaAsync(
        string wabaId, string catalogId, string accessToken, CancellationToken cancellationToken)
    {
        var url =
            $"{GraphBase}/{wabaId}/product_catalogs?fields=id&access_token={Uri.EscapeDataString(accessToken)}";
        using var response = await _http.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return false;

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("data", out var data))
            return false;

        foreach (var catalog in data.EnumerateArray())
        {
            if (catalog.TryGetProperty("id", out var idEl)
                && string.Equals(idEl.GetString(), catalogId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    async Task<IReadOnlyList<string>> GetGrantedScopesAsync(
        string pageToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret))
            return [];

        try
        {
            var appToken = $"{_options.AppId}|{_options.AppSecret}";
            var url =
                $"{GraphBase}/debug_token?input_token={Uri.EscapeDataString(pageToken)}" +
                $"&access_token={Uri.EscapeDataString(appToken)}";
            using var response = await _http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("scopes", out var scopesEl))
            {
                return [];
            }

            return scopesEl.EnumerateArray()
                .Select(s => s.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Cast<string>()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read Meta token scopes");
            return [];
        }
    }

    static MetaRequirementsStatusDto BuildResult(
        string? pageId,
        string? wabaId,
        string? catalogId,
        IReadOnlyList<MetaRequirementCheckDto> checks)
        => new(checks.All(c => c.IsMet), pageId, wabaId, catalogId, checks);

    static MetaRequirementCheckDto Req(
        int step, string id, string title, bool isMet, string status, string fix)
        => new(id, step, title, isMet, status, fix);

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
}
