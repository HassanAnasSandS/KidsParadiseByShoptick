namespace KidsParadiseByShoptick.AdminApp.Services;

public class MetaVideoUploadService : IMetaVideoUploadService
{
    private readonly AdminApiService _api;

    public MetaVideoUploadService(AdminApiService api) => _api = api;

    public async Task UploadAsync(
        Stream videoStream,
        string fileName,
        string title,
        string? caption = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Getting Facebook/Instagram access from server…");
        var credentials = await _api.GetMetaUploadCredentialsAsync();

        progress?.Report("Loading social media settings…");
        var settings = await _api.GetSocialMediaSettingsAsync();
        var postCaption = BuildCaption(title, caption, settings.Description, settings.Tags);

        await using var prepared = await PrepareUploadStreamAsync(videoStream, fileName, cancellationToken);
        await MetaVideoApiClient.UploadToFacebookAndInstagramAsync(
            credentials.FacebookPageId,
            credentials.PageAccessToken,
            credentials.InstagramBusinessAccountId,
            prepared.Stream,
            fileName,
            prepared.Length,
            title,
            postCaption,
            progress,
            cancellationToken);
    }

    static string BuildCaption(string title, string? caption, string? description, string? tags)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(caption))
            parts.Add(caption.Trim());
        else if (!string.IsNullOrWhiteSpace(title))
            parts.Add(title.Trim());

        if (!string.IsNullOrWhiteSpace(description))
            parts.Add(description.Trim());

        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagLine = string.Join(' ',
                tags.Split([',', '\n', '\r', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.TrimStart('#'))
                    .Where(t => t.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(t => $"#{t}"));
            if (!string.IsNullOrWhiteSpace(tagLine))
                parts.Add(tagLine);
        }

        return string.Join("\n\n", parts);
    }

    static async Task<PreparedUploadStream> PrepareUploadStreamAsync(
        Stream videoStream, string fileName, CancellationToken cancellationToken)
    {
        if (videoStream.CanSeek)
        {
            var length = videoStream.Length - videoStream.Position;
            if (length > 0)
                return new PreparedUploadStream(videoStream, length, disposeStream: false, tempPath: null);
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".mp4";

        var tempPath = Path.Combine(
            FileSystem.CacheDirectory,
            $"meta-upload-{Guid.NewGuid():N}{extension}");

        await using (var tempFile = File.Create(tempPath))
            await videoStream.CopyToAsync(tempFile, cancellationToken);

        var length2 = new FileInfo(tempPath).Length;
        var stream = File.OpenRead(tempPath);
        return new PreparedUploadStream(stream, length2, disposeStream: true, tempPath);
    }

    private sealed class PreparedUploadStream : IAsyncDisposable
    {
        private readonly bool _disposeStream;
        private readonly string? _tempPath;

        public PreparedUploadStream(Stream stream, long length, bool disposeStream, string? tempPath)
        {
            Stream = stream;
            Length = length;
            _disposeStream = disposeStream;
            _tempPath = tempPath;
        }

        public Stream Stream { get; }
        public long Length { get; }

        public async ValueTask DisposeAsync()
        {
            if (_disposeStream)
                await Stream.DisposeAsync();

            if (!string.IsNullOrWhiteSpace(_tempPath))
            {
                try { File.Delete(_tempPath); }
                catch { /* ignore */ }
            }
        }
    }
}
