using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KidsParadiseByShoptick.AdminApp.Services;

public static class YouTubeApiClient
{
    private const string UploadScope = "https://www.googleapis.com/auth/youtube.upload";
    public const string DefaultDescription =
        "For Order Whatsapp 0321-7175-896 Or Visit https://kidsparadise.shoptick.shop/";

    public static async Task<string> UploadVideoAsync(
        string accessToken,
        Stream videoStream,
        string fileName,
        string title,
        long contentLength,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromHours(2) };

        progress?.Report("Starting YouTube upload…");

        var metadata = new
        {
            snippet = new
            {
                title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(fileName) : title.Trim(),
                description = DefaultDescription,
                categoryId = "22",
            },
            status = new
            {
                privacyStatus = "public",
            },
        };

        using var initRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status");
        initRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        initRequest.Content = new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8, "application/json");

        using var initResponse = await http.SendAsync(initRequest, cancellationToken);
        var initBody = await initResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!initResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"YouTube upload init failed ({(int)initResponse.StatusCode}): {DescribeApiError(initBody)}");
        }

        var uploadUrl = initResponse.Headers.Location?.ToString();
        if (string.IsNullOrWhiteSpace(uploadUrl))
            throw new InvalidOperationException("YouTube did not return an upload URL.");

        progress?.Report("Uploading video file…");

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
        var streamContent = new StreamContent(videoStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(ResolveVideoContentType(fileName));
        streamContent.Headers.ContentLength = contentLength;
        uploadRequest.Content = streamContent;

        using var uploadResponse = await http.SendAsync(
            uploadRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var responseBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!uploadResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"YouTube upload failed ({(int)uploadResponse.StatusCode}): {DescribeApiError(responseBody)}");
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new InvalidOperationException(
                "YouTube upload finished but returned no video details. Check your Google account connection and try again.");
        }

        string? videoId;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            videoId = doc.RootElement.TryGetProperty("id", out var idEl)
                ? idEl.GetString()
                : null;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"YouTube returned an unexpected response after upload: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(videoId))
            throw new InvalidOperationException("YouTube did not return a video id.");

        progress?.Report("Upload complete.");
        return $"https://www.youtube.com/watch?v={videoId}";
    }

    public static string UploadScopeValue => UploadScope;

    private static string DescribeApiError(string? body) =>
        string.IsNullOrWhiteSpace(body) ? "No error details returned." : body;

    private static string ResolveVideoContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            _ => "application/octet-stream",
        };
    }
}
