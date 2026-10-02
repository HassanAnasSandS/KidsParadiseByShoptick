using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KidsParadiseByShoptick.AdminApp.Services;

/// <summary>
/// Uploads video bytes directly from the Admin app to TikTok (FILE_UPLOAD).
/// Server never receives the video file.
/// </summary>
public static class TikTokApiClient
{
    // TikTok: each middle chunk must be 5–64 MB; whole-file single chunk may be smaller.
    private const long MaxChunkBytes = 64L * 1024 * 1024;
    private const long MinMultiChunkBytes = 5L * 1024 * 1024;

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
        if (contentLength <= 0)
            throw new InvalidOperationException("TikTok upload failed: video file is empty.");

        if (videoStream.CanSeek)
            videoStream.Position = 0;

        using var http = new HttpClient { Timeout = TimeSpan.FromHours(2) };

        // MEDIA_UPLOAD = draft/inbox (video.upload). DIRECT_POST needs approved video.publish.
        var useDirect = string.Equals(postMode, "DIRECT_POST", StringComparison.OrdinalIgnoreCase);

        var (chunkSize, totalChunks) = ResolveChunkPlan(contentLength);

        progress?.Report($"Starting TikTok video upload ({FormatBytes(contentLength)}, {totalChunks} chunk(s))…");

        var videoCaption = string.IsNullOrWhiteSpace(caption) ? Truncate(title, 2200) : Truncate(caption.Trim(), 2200);
        var sourceInfo = new
        {
            source = "FILE_UPLOAD",
            video_size = contentLength,
            chunk_size = chunkSize,
            total_chunk_count = totalChunks,
        };

        string initBody;
        if (useDirect)
        {
            progress?.Report("Initializing TikTok Direct Post (video + caption)…");
            initBody = await InitVideoAsync(
                http,
                "https://open.tiktokapis.com/v2/post/publish/video/init/",
                accessToken,
                new
                {
                    post_mode = "DIRECT_POST",
                    post_info = new
                    {
                        title = videoCaption,
                        privacy_level = string.IsNullOrWhiteSpace(privacyLevel) ? "PUBLIC_TO_EVERYONE" : privacyLevel,
                        disable_duet = false,
                        disable_comment = false,
                        disable_stitch = false,
                        video_cover_timestamp_ms = 1000,
                        brand_organic_toggle = true,
                    },
                    source_info = sourceInfo,
                },
                cancellationToken);
        }
        else
        {
            // Official inbox API: source_info only. post_info.title is ignored by TikTok
            // (unlike photo MEDIA_UPLOAD which does keep description). Still send title
            // in case TikTok starts honoring it; caller copies caption to clipboard.
            progress?.Report("Initializing TikTok Inbox draft…");
            initBody = await InitVideoAsync(
                http,
                "https://open.tiktokapis.com/v2/post/publish/inbox/video/init/",
                accessToken,
                new
                {
                    post_info = new { title = videoCaption },
                    source_info = sourceInfo,
                },
                cancellationToken);
        }

        using var initDoc = JsonDocument.Parse(string.IsNullOrWhiteSpace(initBody) ? "{}" : initBody);
        EnsureOk(initDoc.RootElement);

        if (!initDoc.RootElement.TryGetProperty("data", out var data))
            throw new InvalidOperationException($"TikTok video init missing data: {initBody}");

        var publishId = data.TryGetProperty("publish_id", out var pid) ? pid.GetString() : null;
        var uploadUrl = data.TryGetProperty("upload_url", out var u) ? u.GetString() : null;
        if (string.IsNullOrWhiteSpace(publishId) || string.IsNullOrWhiteSpace(uploadUrl))
            throw new InvalidOperationException($"TikTok did not return publish_id/upload_url: {initBody}");

        var contentType = ResolveContentType(fileName);
        long offset = 0;
        for (var chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
        {
            var thisChunk = chunkIndex == totalChunks - 1
                ? contentLength - offset
                : chunkSize;
            if (thisChunk <= 0)
                break;

            progress?.Report($"Uploading TikTok video chunk {chunkIndex + 1}/{totalChunks}…");

            var buffer = new byte[thisChunk];
            var readTotal = 0;
            while (readTotal < thisChunk)
            {
                var read = await videoStream.ReadAsync(buffer.AsMemory(readTotal, (int)(thisChunk - readTotal)), cancellationToken);
                if (read <= 0)
                    break;
                readTotal += read;
            }

            if (readTotal != thisChunk)
            {
                throw new InvalidOperationException(
                    $"TikTok upload read mismatch: expected {thisChunk} bytes for chunk {chunkIndex + 1}, got {readTotal}.");
            }

            var firstByte = offset;
            var lastByte = offset + thisChunk - 1;

            using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            // TikTok requires Content-Type, Content-Length, Content-Range on the PUT body headers.
            var content = new ByteArrayContent(buffer);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Headers.ContentLength = thisChunk;
            content.Headers.TryAddWithoutValidation("Content-Range", $"bytes {firstByte}-{lastByte}/{contentLength}");
            uploadRequest.Content = content;

            using var uploadResponse = await http.SendAsync(uploadRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var uploadBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
            var code = (int)uploadResponse.StatusCode;

            // TikTok returns 201 (complete) or 206 (partial chunk). 200 is uncommon but accept 2xx.
            if (code is not (200 or 201 or 206) && !uploadResponse.IsSuccessStatusCode)
            {
                var detail = FormatUploadFailure(code, uploadBody, uploadResponse.ReasonPhrase);
                throw new InvalidOperationException($"TikTok video binary upload failed: {detail}");
            }

            offset += thisChunk;
        }

        progress?.Report(useDirect
            ? "TikTok video publish started."
            : "TikTok draft uploaded — open TikTok inbox to finish posting.");

        return publishId;
    }

    static (long ChunkSize, int TotalChunks) ResolveChunkPlan(long videoSize)
    {
        if (videoSize <= MaxChunkBytes)
            return (videoSize, 1);

        var chunkSize = MaxChunkBytes;
        // Keep middle chunks within 5–64 MB; last chunk may be smaller.
        if (chunkSize < MinMultiChunkBytes)
            chunkSize = MinMultiChunkBytes;

        var total = (int)((videoSize + chunkSize - 1) / chunkSize);
        return (chunkSize, Math.Max(1, total));
    }

    static string FormatUploadFailure(int statusCode, string body, string? reason)
    {
        var parsed = ParseError(body);
        var reasonPart = string.IsNullOrWhiteSpace(reason) ? "" : $" {reason}";
        if (string.IsNullOrWhiteSpace(parsed) || parsed is "null" or "TikTok API error.")
        {
            var raw = string.IsNullOrWhiteSpace(body) || body is "null"
                ? "(empty response body)"
                : body.Trim();
            return $"HTTP {statusCode}{reasonPart}: {raw}";
        }

        return $"HTTP {statusCode}{reasonPart}: {parsed}";
    }

    static async Task<string> InitVideoAsync(
        HttpClient http,
        string url,
        string accessToken,
        object payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Content = JsonContent.Create(payload);
        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode || !IsTikTokOk(body))
        {
            throw new InvalidOperationException(
                $"TikTok video init failed (HTTP {(int)response.StatusCode}): {ParseError(body)}");
        }

        return body;
    }

    static bool IsTikTokOk(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("error", out var error))
                return doc.RootElement.TryGetProperty("data", out _);
            if (!error.TryGetProperty("code", out var code))
                return false;
            return string.Equals(code.GetString(), "ok", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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

    static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / (1024.0 * 1024.0):0.#} MB";
    }

    static string ParseError(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Trim() is "null")
            return "TikTok API error.";

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var code = error.TryGetProperty("code", out var c) ? c.GetString() : null;
                var message = error.TryGetProperty("message", out var m) ? m.GetString() : null;
                if (!string.IsNullOrWhiteSpace(message))
                    return string.IsNullOrWhiteSpace(code) ? message! : $"{code}: {message}";
                if (!string.IsNullOrWhiteSpace(code) && !string.Equals(code, "ok", StringComparison.OrdinalIgnoreCase))
                    return code!;
            }

            if (doc.RootElement.TryGetProperty("message", out var topMsg))
            {
                var msg = topMsg.GetString();
                if (!string.IsNullOrWhiteSpace(msg))
                    return msg!;
            }
        }
        catch
        {
            // ignored
        }

        return body.Trim();
    }
}
