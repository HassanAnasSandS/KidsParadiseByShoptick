using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KidsParadiseByShoptick.AdminApp.Config;
using KidsParadiseByShoptick.AdminApp.Helpers;
using KidsParadiseByShoptick.AdminApp.Models;

namespace KidsParadiseByShoptick.AdminApp.Services;

public class AdminApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _http;
    private readonly AuthSession _session;

    public AdminApiService(AuthSession session)
    {
        _session = session;
        _http = new HttpClient
        {
            BaseAddress = new Uri(AppSettings.ApiBaseUrl.TrimEnd('/') + "/"),
            // Video uploads to server / social can take a long time on slow links.
            Timeout = TimeSpan.FromHours(2),
        };
        _session.SessionChanged += ApplyAuthHeader;
        ApplyAuthHeader();
    }

    private void ApplyAuthHeader()
    {
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(_session.Token)
            ? null
            : new AuthenticationHeaderValue("Bearer", _session.Token);
    }

    public async Task<AdminLoginResponse> LoginAsync(string username, string password, bool rememberMe)
    {
        var payload = new { username, password, rememberMe };
        using var res = await _http.PostAsJsonAsync("admin/auth/login", payload, JsonOptions);
        if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Invalid username or password.");
        await EnsureSuccessAsync(res);
        var result = await res.Content.ReadFromJsonAsync<AdminLoginResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Empty login response.");
        await _session.SaveAsync(result.Token, result.Username, rememberMe);
        return result;
    }

    public async Task LogoutAsync() => await _session.ClearAsync();

    public Task<DashboardModel> GetDashboardAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var query = new List<string>();
        if (dateFrom.HasValue)
            query.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue)
            query.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
        var qs = query.Count > 0 ? "?" + string.Join('&', query) : string.Empty;
        return GetAsync<DashboardModel>($"admin/dashboard{qs}");
    }

    public Task<PagedResult<CategoryModel>> GetCategoriesPagedAsync(
        int page, int pageSize, string? search = null, string? toyFilter = null, string? sort = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(toyFilter))
            query.Add($"toyFilter={Uri.EscapeDataString(toyFilter)}");
        if (!string.IsNullOrWhiteSpace(sort))
            query.Add($"sort={Uri.EscapeDataString(sort)}");
        return GetAsync<PagedResult<CategoryModel>>($"admin/categories?{string.Join('&', query)}");
    }

    public async Task<List<CategoryModel>> GetCategoriesAsync()
    {
        var result = await GetCategoriesPagedAsync(1, 500);
        return CategoryNameSort.OrderByDisplayName(result.Items, c => c.Name).ToList();
    }

    public Task<CategoryModel> CreateCategoryAsync(string name, string? imagePath) =>
        PostAsync<CategoryModel>("admin/categories", new { name, imagePath });

    public Task<CategoryModel> UpdateCategoryAsync(int id, string name, string? imagePath) =>
        PutAsync<CategoryModel>($"admin/categories/{id}", new { name, imagePath });

    public Task DeleteCategoryAsync(int id) => DeleteAsync($"admin/categories/{id}");

    public async Task<List<ToyListModel>> GetToysAsync()
    {
        var result = await GetToysPagedAsync(1, 50, isSold: false, search: null);
        return result.Items;
    }

    public Task<PagedResult<ToyListModel>> GetToysPagedAsync(
        int page, int pageSize, int? categoryId = null, string? search = null,
        bool? isSold = null, bool? onSale = null, string? sort = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (categoryId.HasValue)
            query.Add($"categoryId={categoryId.Value}");
        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (isSold.HasValue)
            query.Add($"isSold={isSold.Value.ToString().ToLowerInvariant()}");
        if (onSale.HasValue)
            query.Add($"onSale={onSale.Value.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrWhiteSpace(sort))
            query.Add($"sort={Uri.EscapeDataString(sort)}");
        return GetAsync<PagedResult<ToyListModel>>($"admin/toys?{string.Join('&', query)}");
    }

    public Task<ToyDetailModel> GetToyAsync(int id) => GetAsync<ToyDetailModel>($"admin/toys/{id}");

    public Task<AdminToySaveResponseModel> CreateToyAsync(object payload) =>
        PostAsync<AdminToySaveResponseModel>("admin/toys", payload);

    public Task<ToyListModel> CloneToyAsync(int id) =>
        PostAsync<ToyListModel>($"admin/toys/{id}/clone", new { });

    public Task<AdminToySaveResponseModel> UpdateToyAsync(int id, object payload) =>
        PutAsync<AdminToySaveResponseModel>($"admin/toys/{id}", payload);

    public Task DeleteToyAsync(int id) => DeleteAsync($"admin/toys/{id}");

    public async Task<List<ToyImageSearchMatchModel>> SearchToysByImageAsync(Stream stream, string fileName, int limit = 20)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", fileName);
        using var res = await _http.PostAsync($"admin/toys/search-by-image?limit={limit}", content);
        await EnsureSuccessAsync(res);
        return await res.Content.ReadFromJsonAsync<List<ToyImageSearchMatchModel>>(JsonOptions)
            ?? [];
    }

    public Task<ToyImageIndexResultModel> IndexToyImagesAsync() =>
        PostAsync<ToyImageIndexResultModel>("admin/toys/index-images", new { });

    public Task<OrderStatusCountsModel> GetOrderStatusCountsAsync() =>
        GetAsync<OrderStatusCountsModel>("admin/orders/status-counts");

    public async Task<(string? AccessToken, string? AuthUrl)> GetYouTubeAccessTokenAsync()
    {
        using var res = await _http.GetAsync("admin/youtube/access-token");
        var body = await res.Content.ReadAsStringAsync();

        if (res.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException("Server returned an empty YouTube token response.");

            YouTubeAccessTokenResponse? data;
            try
            {
                data = JsonSerializer.Deserialize<YouTubeAccessTokenResponse>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid YouTube token response from server: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(data?.AccessToken))
                throw new InvalidOperationException("Server did not return a YouTube access token.");

            return (data.AccessToken, null);
        }

        if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var data = JsonSerializer.Deserialize<YouTubeAuthRequiredResponse>(body, JsonOptions);
                if (data?.NeedsAuth == true && !string.IsNullOrWhiteSpace(data.AuthUrl))
                    return (null, data.AuthUrl);

                if (!string.IsNullOrWhiteSpace(data?.Message))
                    throw new InvalidOperationException(data.Message);
            }
            catch (JsonException)
            {
                // Fall through to generic handler below.
            }
        }

        if (res.StatusCode == System.Net.HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var data = JsonSerializer.Deserialize<YouTubeAuthRequiredResponse>(body, JsonOptions);
                if (!string.IsNullOrWhiteSpace(data?.Message))
                    throw new InvalidOperationException(data.Message);
            }
            catch (JsonException)
            {
                // Fall through to generic handler below.
            }
        }

        await EnsureSuccessAsync(res, body);
        return (null, null);
    }

    public Task<PagedResult<OrderModel>> GetOrdersPagedAsync(
        int page, int pageSize, string? status = null, string? search = null, string? sort = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            query.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(sort))
            query.Add($"sort={Uri.EscapeDataString(sort)}");
        return GetAsync<PagedResult<OrderModel>>($"admin/orders?{string.Join('&', query)}");
    }

    public Task<PagedResult<ReviewModel>> GetReviewsPagedAsync(
        int page, int pageSize, string? search = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        return GetAsync<PagedResult<ReviewModel>>($"admin/reviews?{string.Join('&', query)}");
    }

    public Task<OrderModel> GetOrderAsync(int id) => GetAsync<OrderModel>($"admin/orders/{id}");

    public Task<OrderPlacedModel> CreateOrderAsync(object payload) =>
        PostAsync<OrderPlacedModel>("admin/orders", payload);

    public Task<OrderModel> UpdateOrderAsync(int id, object payload) =>
        PutAsync<OrderModel>($"admin/orders/{id}", payload);

    public Task<OrderModel> UpdateOrderStatusAsync(int id, object payload) =>
        PatchAsync<OrderModel>($"admin/orders/{id}/status", payload);

    public Task DeleteOrderAsync(int id) => DeleteAsync($"admin/orders/{id}");

    public async Task<List<ReviewModel>> GetReviewsAsync()
    {
        var result = await GetReviewsPagedAsync(1, 500);
        return result.Items;
    }

    public Task<ReviewModel> UpdateReviewAsync(int id, object payload) =>
        PutAsync<ReviewModel>($"admin/reviews/{id}", payload);

    public async Task<List<SiteImageModel>> GetSiteImagesAsync()
    {
        var result = await GetAsync<PagedResult<SiteImageModel>>("admin/site-images?page=1&pageSize=500");
        return result.Items;
    }

    public Task<SiteImageModel> ResetSiteImageAsync(string key) =>
        DeleteWithBodyAsync<SiteImageModel>($"admin/site-images/{key}/custom");

    public Task<SiteImageModel> UpdateSiteImageContentAsync(
        string key,
        string? title,
        string? subtitle,
        string? ctaText,
        string? linkUrl,
        string? titleColor = null,
        string? subtitleColor = null,
        string? ctaColor = null) =>
        PutAsync<SiteImageModel>($"admin/site-images/{Uri.EscapeDataString(key)}/content", new
        {
            title,
            subtitle,
            ctaText,
            linkUrl,
            titleColor,
            subtitleColor,
            ctaColor,
        });

    public Task<DeliveryChargeSettingsModel> GetDeliveryChargesAsync() =>
        GetAsync<DeliveryChargeSettingsModel>("admin/delivery-charges");

    public Task<DeliveryChargeSettingsModel> UpdateDeliveryChargesAsync(decimal karachi, decimal otherCities) =>
        PutAsync<DeliveryChargeSettingsModel>("admin/delivery-charges", new { karachi, otherCities });

    public Task<SiteSocialLinksModel> GetSiteSocialLinksAsync() =>
        GetAsync<SiteSocialLinksModel>("admin/site-social-links");

    public Task<SiteSocialLinksModel> UpdateSiteSocialLinksAsync(SiteSocialLinksModel links) =>
        PutAsync<SiteSocialLinksModel>("admin/site-social-links", new
        {
            whatsAppNumber = links.WhatsAppNumber,
            whatsAppDisplay = links.WhatsAppDisplay,
            youTubeUrl = links.YouTubeUrl,
            facebookUrl = links.FacebookUrl,
            instagramUrl = links.InstagramUrl,
            tikTokUrl = links.TikTokUrl,
            pinterestUrl = links.PinterestUrl,
        });

    public Task<PagedResult<AffiliatePartnerModel>> GetAffiliatesPagedAsync(
        int page, int pageSize, string? search = null, bool? isActive = null)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (isActive.HasValue)
            query.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        return GetAsync<PagedResult<AffiliatePartnerModel>>($"admin/affiliates?{string.Join('&', query)}");
    }

    public async Task<List<AffiliatePartnerModel>> GetAffiliatesAsync(bool? isActive = true)
    {
        var result = await GetAffiliatesPagedAsync(1, 500, isActive: isActive);
        return result.Items;
    }

    public Task<AffiliatePartnerModel> GetAffiliateAsync(int id) =>
        GetAsync<AffiliatePartnerModel>($"admin/affiliates/{id}");

    public Task<AffiliatePartnerModel> CreateAffiliateAsync(object payload) =>
        PostAsync<AffiliatePartnerModel>("admin/affiliates", payload);

    public Task<AffiliatePartnerModel> UpdateAffiliateAsync(int id, object payload) =>
        PutAsync<AffiliatePartnerModel>($"admin/affiliates/{id}", payload);

    public Task DeleteAffiliateAsync(int id) => DeleteAsync($"admin/affiliates/{id}");

    public Task<AffiliateLedgerModel> GetAffiliateLedgerAsync(int id) =>
        GetAsync<AffiliateLedgerModel>($"admin/affiliates/{id}/ledger");

    public Task<AffiliateLedgerEntryModel> RecordAffiliatePaymentAsync(int id, decimal amount, string? notes) =>
        PostAsync<AffiliateLedgerEntryModel>($"admin/affiliates/{id}/payments", new { amount, notes });

    public Task<SocialMediaSettingsModel> GetSocialMediaSettingsAsync() =>
        GetAsync<SocialMediaSettingsModel>("admin/social-media-settings");

    public Task<SocialMediaSettingsModel> UpdateSocialMediaSettingsAsync(SocialMediaSettingsModel settings) =>
        PutAsync<SocialMediaSettingsModel>("admin/social-media-settings", new
        {
            description = settings.Description,
            tags = settings.Tags,
            tikTokPostMode = settings.TikTokPostMode,
            onCreate = settings.OnCreate,
            onEdit = settings.OnEdit,
        });

    public Task<SocialMediaSettingsModel> UpdateSocialMediaSettingsAsync(string description, string tags, string? tikTokPostMode = null) =>
        PutAsync<SocialMediaSettingsModel>("admin/social-media-settings", new
        {
            description,
            tags,
            tikTokPostMode,
        });

    public Task<MetaRequirementsStatusModel> GetMetaRequirementsAsync() =>
        GetAsync<MetaRequirementsStatusModel>("admin/meta/requirements");

    public Task<MetaConnectionStatusModel> GetMetaStatusAsync() =>
        GetAsync<MetaConnectionStatusModel>("admin/meta/status");

    public async Task ConnectMetaAsync(string userAccessToken, string? facebookPageId = null, string? whatsAppCatalogId = null)
    {
        using var res = await _http.PostAsJsonAsync("admin/meta/connect", new
        {
            userAccessToken,
            facebookPageId,
            whatsAppCatalogId,
        }, JsonOptions);
        await EnsureSuccessAsync(res);
    }

    public Task DisconnectMetaAsync() =>
        DeleteAsync("admin/meta/disconnect");

    public Task<MetaRequirementsStatusModel> LinkWhatsAppCatalogAsync(string catalogId) =>
        PostAsync<MetaRequirementsStatusModel>("admin/meta/link-catalog", new { catalogId });

    public async Task<MetaUploadCredentialsModel> GetMetaUploadCredentialsAsync()
    {
        using var res = await _http.GetAsync("admin/meta/upload-credentials");
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var message))
                    throw new InvalidOperationException(message.GetString() ?? body);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                // fall through
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body)
                    ? $"Meta credentials request failed ({(int)res.StatusCode})."
                    : body);
        }

        var data = JsonSerializer.Deserialize<MetaUploadCredentialsModel>(body, JsonOptions)
            ?? throw new InvalidOperationException("Server returned empty Meta upload credentials.");

        if (string.IsNullOrWhiteSpace(data.FacebookPageId) || string.IsNullOrWhiteSpace(data.PageAccessToken))
            throw new InvalidOperationException("Server did not return a Facebook page access token.");

        return data;
    }

    public Task<TikTokStatusModel> GetTikTokStatusAsync() =>
        GetAsync<TikTokStatusModel>("admin/tiktok/status");

    public Task<TikTokStatusModel> UpdateTikTokPostModeAsync(string postMode) =>
        PutAsync<TikTokStatusModel>("admin/tiktok/post-mode", new { postMode });

    public async Task<string> GetTikTokAuthUrlAsync(string? postMode = null)
    {
        var path = string.IsNullOrWhiteSpace(postMode)
            ? "admin/tiktok/auth-url"
            : $"admin/tiktok/auth-url?postMode={Uri.EscapeDataString(postMode)}";
        using var res = await _http.GetAsync(path);
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var message))
                    throw new InvalidOperationException(message.GetString() ?? body);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                // fall through
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body)
                    ? $"TikTok auth URL request failed ({(int)res.StatusCode})."
                    : body);
        }

        var data = JsonSerializer.Deserialize<TikTokAuthUrlModel>(body, JsonOptions)
            ?? throw new InvalidOperationException("Server returned empty TikTok auth URL.");
        if (string.IsNullOrWhiteSpace(data.Url))
            throw new InvalidOperationException("Server did not return a TikTok auth URL.");
        return data.Url;
    }

    public Task DisconnectTikTokAsync() =>
        PostAsync<TikTokStatusModel>("admin/tiktok/disconnect", new { });

    public Task<PinterestStatusModel> GetPinterestStatusAsync() =>
        GetAsync<PinterestStatusModel>("admin/pinterest/status");

    public async Task<string> GetPinterestAuthUrlAsync()
    {
        using var res = await _http.GetAsync("admin/pinterest/auth-url");
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var message))
                    throw new InvalidOperationException(message.GetString() ?? body);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                // fall through
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body)
                    ? $"Pinterest auth URL request failed ({(int)res.StatusCode})."
                    : body);
        }

        var data = JsonSerializer.Deserialize<PinterestAuthUrlModel>(body, JsonOptions)
            ?? throw new InvalidOperationException("Server returned empty Pinterest auth URL.");
        if (string.IsNullOrWhiteSpace(data.Url))
            throw new InvalidOperationException("Server did not return a Pinterest auth URL.");
        return data.Url;
    }

    public Task DisconnectPinterestAsync() =>
        PostAsync<PinterestStatusModel>("admin/pinterest/disconnect", new { });

    public Task<YouTubeConnectionStatusModel> GetYouTubeStatusAsync() =>
        GetAsync<YouTubeConnectionStatusModel>("admin/youtube/status");

    public async Task<string> GetYouTubeAuthUrlAsync()
    {
        using var res = await _http.GetAsync("admin/youtube/auth-url");
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var message))
                    throw new InvalidOperationException(message.GetString() ?? body);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                // fall through
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body)
                    ? $"YouTube auth URL request failed ({(int)res.StatusCode})."
                    : body);
        }

        var data = JsonSerializer.Deserialize<YouTubeAuthUrlModel>(body, JsonOptions)
            ?? throw new InvalidOperationException("Server returned empty YouTube auth URL.");
        if (string.IsNullOrWhiteSpace(data.Url))
            throw new InvalidOperationException("Server did not return a YouTube auth URL.");
        return data.Url;
    }

    public Task DisconnectYouTubeAsync() =>
        DeleteAsync("admin/youtube/disconnect");

    /// <summary>
    /// Returns TikTok upload credentials. When not connected, AccessToken is empty and AuthUrl may be set.
    /// </summary>
    public async Task<TikTokAccessTokenModel> GetTikTokAccessTokenAsync()
    {
        using var res = await _http.GetAsync("admin/tiktok/access-token");
        var body = await res.Content.ReadAsStringAsync();

        if (res.IsSuccessStatusCode)
        {
            var data = JsonSerializer.Deserialize<TikTokAccessTokenModel>(body, JsonOptions)
                ?? throw new InvalidOperationException("Server returned empty TikTok token response.");
            if (string.IsNullOrWhiteSpace(data.AccessToken))
                throw new InvalidOperationException("Server did not return a TikTok access token.");
            return data;
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var err = JsonSerializer.Deserialize<TikTokAccessTokenModel>(body, JsonOptions);
                if (err is not null)
                    return err;
            }
            catch (JsonException)
            {
                // fall through
            }
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(body)
                ? $"TikTok token request failed ({(int)res.StatusCode})."
                : body);
    }

    public async Task<UploadResult> UploadAsync(Stream stream, string fileName, string folder)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", fileName);
        using var res = await _http.PostAsync($"admin/upload?folder={Uri.EscapeDataString(folder)}", content);
        await EnsureSuccessAsync(res);
        return await res.Content.ReadFromJsonAsync<UploadResult>(JsonOptions)
            ?? throw new InvalidOperationException("Upload failed.");
    }

    public async Task<UploadResult> UploadVideoAsync(Stream stream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        content.Add(streamContent, "file", fileName);

        using var req = new HttpRequestMessage(HttpMethod.Post, "admin/upload/video") { Content = content };
        using var cts = new CancellationTokenSource(TimeSpan.FromHours(2));
        using var res = await _http.SendAsync(req, cts.Token);
        await EnsureSuccessAsync(res);
        return await res.Content.ReadFromJsonAsync<UploadResult>(JsonOptions)
            ?? throw new InvalidOperationException("Video upload failed.");
    }

    public async Task<SiteImageModel> UploadSiteImageAsync(string key, Stream stream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", fileName);
        using var res = await _http.PostAsync($"admin/site-images/{Uri.EscapeDataString(key)}/upload", content);
        await EnsureSuccessAsync(res);
        return await res.Content.ReadFromJsonAsync<SiteImageModel>(JsonOptions)
            ?? throw new InvalidOperationException("Upload failed.");
    }

    private async Task<T> GetAsync<T>(string url)
    {
        using var res = await _http.GetAsync(url);
        await EnsureSuccessAsync(res);
        return await res.Content.ReadFromJsonAsync<T>(JsonOptions) ?? Activator.CreateInstance<T>();
    }

    private async Task<T> PostAsync<T>(string url, object payload)
    {
        using var res = await _http.PostAsJsonAsync(url, payload, JsonOptions);
        await EnsureSuccessAsync(res);
        return await res.Content.ReadFromJsonAsync<T>(JsonOptions)
            ?? throw new InvalidOperationException("Empty response.");
    }

    private async Task<T> PutAsync<T>(string url, object payload)
    {
        using var res = await _http.PutAsJsonAsync(url, payload, JsonOptions);
        await EnsureSuccessAsync(res);
        return await res.Content.ReadFromJsonAsync<T>(JsonOptions)
            ?? throw new InvalidOperationException("Empty response.");
    }

    private async Task<T> PatchAsync<T>(string url, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var req = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        using var res = await _http.SendAsync(req);
        await EnsureSuccessAsync(res);
        return await res.Content.ReadFromJsonAsync<T>(JsonOptions)
            ?? throw new InvalidOperationException("Empty response.");
    }

    private async Task DeleteAsync(string url)
    {
        using var res = await _http.DeleteAsync(url);
        await EnsureSuccessAsync(res);
    }

    private async Task<T> DeleteWithBodyAsync<T>(string url)
    {
        using var res = await _http.DeleteAsync(url);
        await EnsureSuccessAsync(res);
        return await res.Content.ReadFromJsonAsync<T>(JsonOptions)
            ?? throw new InvalidOperationException("Empty response.");
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage res, string? body = null)
    {
        if (res.IsSuccessStatusCode) return;

        if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Do not wipe a saved session when the request was sent without a token (startup race).
            if (_http.DefaultRequestHeaders.Authorization is not null)
                await _session.ClearAsync();
            throw new UnauthorizedAccessException("Session expired. Please sign in again.");
        }

        body ??= await res.Content.ReadAsStringAsync();
        string? message = null;
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var err = JsonSerializer.Deserialize<ApiError>(body, JsonOptions);
                message = err?.Message;
            }
            catch (JsonException)
            {
                message = body.Length > 300 ? body[..300] : body;
            }
        }

        throw new InvalidOperationException(message ?? $"Request failed ({(int)res.StatusCode}).");
    }
}
