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
        if (!initResponse.IsSuccessStatusCode)
        {
            var error = await initResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"YouTube upload init failed: {error}");
        }

        var uploadUrl = initResponse.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("YouTube did not return an upload URL.");

        progress?.Report("Uploading video file…");

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
        uploadRequest.Content = new StreamContent(videoStream);
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(ResolveVideoContentType(fileName));

        using var uploadResponse = await http.SendAsync(
            uploadRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!uploadResponse.IsSuccessStatusCode)
        {
            var error = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"YouTube upload failed: {error}");
        }

        await using var responseStream = await uploadResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var videoId = doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("YouTube did not return a video id.");

        progress?.Report("Upload complete.");
        return $"https://www.youtube.com/watch?v={videoId}";
    }

    public static string UploadScopeValue => UploadScope;

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
