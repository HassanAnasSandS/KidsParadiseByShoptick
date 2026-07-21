using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KidsParadiseByShoptick.AdminApp.Services;

/// <summary>
/// Uploads video bytes directly from the Admin app to TikTok (FILE_UPLOAD). Server never receives the video file.
/// </summary>
public static class TikTokApiClient
{
    public static async Task<string> UploadVideoAsync(
        string accessToken,
        string postMode,
        string privacyLevel,
        Stream videoStream,
        string fileName,
        long contentLength,
        string title,
        string? caption,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromHours(2) };

        var useDirect = string.Equals(postMode, "DIRECT_POST", StringComparison.OrdinalIgnoreCase);
        var initUrl = useDirect
            ? "https://open.tiktokapis.com/v2/post/publish/video/init/"
            : "https://open.tiktokapis.com/v2/post/publish/inbox/video/init/";

        progress?.Report("Starting TikTok video upload…");

        object payload;
        if (useDirect)
        {
            payload = new
            {
                post_info = new
                {
                    title = Truncate(title, 150),
                    privacy_level = string.IsNullOrWhiteSpace(privacyLevel) ? "SELF_ONLY" : privacyLevel,
                    disable_duet = false,
                    disable_comment = false,
                    disable_stitch = false,
                    video_cover_timestamp_ms = 1000,
                },
                source_info = new
                {
                    source = "FILE_UPLOAD",
                    video_size = contentLength,
                    chunk_size = contentLength,
                    total_chunk_count = 1,
                },
            };
        }
        else
        {
            payload = new
            {
                source_info = new
                {
                    source = "FILE_UPLOAD",
                    video_size = contentLength,
                    chunk_size = contentLength,
                    total_chunk_count = 1,
                },
            };
        }

        using var initRequest = new HttpRequestMessage(HttpMethod.Post, initUrl);
        initRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        initRequest.Content = JsonContent.Create(payload);

        using var initResponse = await http.SendAsync(initRequest, cancellationToken);
        var initBody = await initResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!initResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"TikTok video init failed: {ParseError(initBody)}");

        using var initDoc = JsonDocument.Parse(initBody);
        EnsureOk(initDoc.RootElement);

        if (!initDoc.RootElement.TryGetProperty("data", out var data))
            throw new InvalidOperationException($"TikTok video init missing data: {initBody}");

        var publishId = data.TryGetProperty("publish_id", out var pid) ? pid.GetString() : null;
        var uploadUrl = data.TryGetProperty("upload_url", out var u) ? u.GetString() : null;
        if (string.IsNullOrWhiteSpace(publishId) || string.IsNullOrWhiteSpace(uploadUrl))
            throw new InvalidOperationException($"TikTok did not return publish_id/upload_url: {initBody}");

        progress?.Report("Uploading video file to TikTok…");
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
        uploadRequest.Headers.TryAddWithoutValidation("Content-Range", $"bytes 0-{contentLength - 1}/{contentLength}");
        uploadRequest.Content = new StreamContent(videoStream);
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(ResolveContentType(fileName));
        uploadRequest.Content.Headers.ContentLength = contentLength;

        using var uploadResponse = await http.SendAsync(uploadRequest, cancellationToken);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!uploadResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"TikTok video binary upload failed: {ParseError(uploadBody)}");

        progress?.Report(useDirect
            ? "TikTok video publish started."
            : "TikTok draft uploaded — open TikTok inbox to finish posting.");

        // Caption/title for inbox mode is completed in TikTok app; direct mode uses title above.
        _ = caption;
        return publishId;
    }

    static void EnsureOk(JsonElement root)
    {
        if (!root.TryGetProperty("error", out var error))
            return;
        if (error.TryGetProperty("code", out var code) &&
            string.Equals(code.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
            return;
        throw new InvalidOperationException(ParseError(root.GetRawText()));
    }

    static string Truncate(string value, int max) =>
        string.IsNullOrWhiteSpace(value) ? "Kids Paradise" :
        value.Length <= max ? value.Trim() : value.Trim()[..max];

    static string ResolveContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            _ => "video/mp4",
        };
    }

    static string ParseError(string body)
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
