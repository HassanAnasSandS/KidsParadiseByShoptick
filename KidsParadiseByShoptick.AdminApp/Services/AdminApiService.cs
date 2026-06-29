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
            Timeout = TimeSpan.FromMinutes(2),
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
