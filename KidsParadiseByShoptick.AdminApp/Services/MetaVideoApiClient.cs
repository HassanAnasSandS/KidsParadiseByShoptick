using System.Net.Http.Headers;
using System.Text.Json;

namespace KidsParadiseByShoptick.AdminApp.Services;

/// <summary>
/// Uploads video bytes directly from the Admin app to Facebook Page + Instagram (Reels).
/// Server is not involved in transferring the video file.
/// </summary>
public static class MetaVideoApiClient
{
    private const string GraphVersion = "v21.0";
    private const string GraphBase = "https://graph.facebook.com/" + GraphVersion;
    private const string GraphVideoBase = "https://graph-video.facebook.com/" + GraphVersion;

    public static async Task UploadToFacebookAndInstagramAsync(
        string facebookPageId,
        string pageAccessToken,
        string? instagramBusinessAccountId,
        Stream videoStream,
        string fileName,
        long contentLength,
        string title,
        string? caption,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromHours(2) };
        var postCaption = string.IsNullOrWhiteSpace(caption) ? title : caption.Trim();
        // Instagram captions should be plain text (photo-style HTML tags break Promote UX / look wrong on IG).
        var instagramCaption = StripHtml(postCaption);

        // Always copy to a temp file and open a FRESH stream per platform.
        // Multipart StreamContent disposes the underlying stream after Facebook upload,
        // which caused ObjectDisposed_FileClosed on Instagram.
        var tempPath = await CopyToTempFileAsync(videoStream, fileName, contentLength, cancellationToken);
        try
        {
            var length = new FileInfo(tempPath).Length;

            progress?.Report("Uploading video to Facebook…");
            await using (var fbStream = File.OpenRead(tempPath))
            {
                await UploadFacebookPageVideoAsync(
                    http, facebookPageId, pageAccessToken, fbStream, fileName, length, title, postCaption, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(instagramBusinessAccountId))
            {
                progress?.Report("Uploading video to Instagram…");
                await using (var igStream = File.OpenRead(tempPath))
                {
                    await UploadInstagramReelAsync(
                        http, instagramBusinessAccountId, pageAccessToken, igStream, length, instagramCaption, progress, cancellationToken);
                }
            }
            else
            {
                progress?.Report("Instagram skipped (Business account not linked).");
            }

            progress?.Report("Facebook and Instagram video posted.");
        }
        finally
        {
            try { File.Delete(tempPath); }
            catch { /* ignore */ }
        }
    }

    static async Task<string> CopyToTempFileAsync(
        Stream videoStream, string fileName, long contentLength, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".mp4";

        var tempPath = Path.Combine(
            FileSystem.CacheDirectory,
            $"meta-video-{Guid.NewGuid():N}{extension}");

        await using (var tempFile = File.Create(tempPath))
        {
            if (videoStream.CanSeek)
                videoStream.Position = 0;

            await videoStream.CopyToAsync(tempFile, cancellationToken);
            await tempFile.FlushAsync(cancellationToken);
        }

        var length = new FileInfo(tempPath).Length;
        if (length <= 0)
            throw new InvalidOperationException("Video file is empty — cannot upload.");

        // contentLength from caller may be stale; file length is authoritative
        _ = contentLength;
        return tempPath;
    }

    static async Task UploadFacebookPageVideoAsync(
        HttpClient http,
        string pageId,
        string accessToken,
        Stream videoStream,
        string fileName,
        long contentLength,
        string title,
        string description,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(accessToken), "access_token");
        content.Add(new StringContent(title), "title");
        content.Add(new StringContent(description), "description");
        content.Add(new StringContent("true"), "published");

        var streamContent = new StreamContent(videoStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(ResolveVideoContentType(fileName));
        streamContent.Headers.ContentLength = contentLength;
        content.Add(streamContent, "source", fileName);

        using var response = await http.PostAsync($"{GraphVideoBase}/{pageId}/videos", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Facebook video upload failed: {ParseGraphError(body)}");

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("id", out _))
            throw new InvalidOperationException($"Facebook video upload returned unexpected response: {body}");
    }

    static async Task UploadInstagramReelAsync(
        HttpClient http,
        string igUserId,
        string accessToken,
        Stream videoStream,
        long contentLength,
        string caption,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        using var initContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["media_type"] = "REELS",
            ["upload_type"] = "resumable",
            ["caption"] = caption,
            // Show in profile Feed as well as Reels — improves Promote/Boost visibility in Instagram app.
            ["share_to_feed"] = "true",
            ["access_token"] = accessToken,
        });

        using var initResponse = await http.PostAsync($"{GraphBase}/{igUserId}/media", initContent, cancellationToken);
        var initBody = await initResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!initResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Instagram video init failed: {ParseGraphError(initBody)}");

        using var initDoc = JsonDocument.Parse(initBody);
        var containerId = initDoc.RootElement.TryGetProperty("id", out var idEl)
            ? idEl.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(containerId))
            throw new InvalidOperationException($"Instagram did not return a container id: {initBody}");

        var uploadUri = initDoc.RootElement.TryGetProperty("uri", out var uriEl)
            ? uriEl.GetString()
            : $"https://rupload.facebook.com/ig-api-upload/{GraphVersion}/{containerId}";

        progress?.Report("Sending video file to Instagram…");
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUri);
        uploadRequest.Headers.TryAddWithoutValidation("Authorization", $"OAuth {accessToken}");
        uploadRequest.Headers.TryAddWithoutValidation("offset", "0");
        uploadRequest.Headers.TryAddWithoutValidation("file_size", contentLength.ToString());
        uploadRequest.Content = new StreamContent(videoStream);
        uploadRequest.Content.Headers.ContentLength = contentLength;
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var uploadResponse = await http.SendAsync(uploadRequest, cancellationToken);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!uploadResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Instagram video binary upload failed: {ParseGraphError(uploadBody)}");

        progress?.Report("Waiting for Instagram to process video…");
        await WaitForInstagramContainerAsync(http, containerId, accessToken, cancellationToken);

        progress?.Report("Publishing Instagram Reel…");
        using var publishContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["creation_id"] = containerId,
            ["access_token"] = accessToken,
        });
        using var publishResponse = await http.PostAsync($"{GraphBase}/{igUserId}/media_publish", publishContent, cancellationToken);
        var publishBody = await publishResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!publishResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Instagram video publish failed: {ParseGraphError(publishBody)}");
    }

    static async Task WaitForInstagramContainerAsync(
        HttpClient http, string containerId, string accessToken, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddMinutes(10);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var url =
                $"{GraphBase}/{containerId}?fields=status_code,status&access_token={Uri.EscapeDataString(accessToken)}";
            using var response = await http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Instagram status check failed: {ParseGraphError(body)}");

            using var doc = JsonDocument.Parse(body);
            var status = doc.RootElement.TryGetProperty("status_code", out var statusEl)
                ? statusEl.GetString()
                : null;

            if (string.Equals(status, "FINISHED", StringComparison.OrdinalIgnoreCase))
                return;

            if (string.Equals(status, "ERROR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "EXPIRED", StringComparison.OrdinalIgnoreCase))
            {
                var detail = doc.RootElement.TryGetProperty("status", out var st)
                    ? st.GetString()
                    : body;
                throw new InvalidOperationException($"Instagram video processing failed: {detail}");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }

        throw new InvalidOperationException("Instagram video processing timed out. Try again with a shorter video.");
    }

    static string StripHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return System.Text.RegularExpressions.Regex
            .Replace(value, "<.*?>", string.Empty)
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    static string ResolveVideoContentType(string fileName)
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

        return string.IsNullOrWhiteSpace(body) ? "No error details returned." : body;
    }
}
