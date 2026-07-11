namespace KidsParadiseByShoptick.AdminApp.Services;

public class YouTubeUploadService : IYouTubeUploadService
{
    private readonly AdminApiService _api;

    public YouTubeUploadService(AdminApiService api) => _api = api;

    public bool IsSupported => true;

    public async Task<string> UploadAsync(
        Stream videoStream,
        string fileName,
        string title,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Getting YouTube access from server…");
        var (accessToken, authUrl) = await _api.GetYouTubeAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            if (!string.IsNullOrWhiteSpace(authUrl))
            {
                await Launcher.OpenAsync(new Uri(authUrl));
                throw new InvalidOperationException(
                    "YouTube is not connected yet. Complete Google sign-in in the browser, then tap Upload again.");
            }

            throw new InvalidOperationException("Could not get YouTube access token from the server.");
        }

        progress?.Report("Loading social media settings…");
        var settings = await _api.GetSocialMediaSettingsAsync();

        await using var prepared = await PrepareUploadStreamAsync(videoStream, fileName, cancellationToken);
        return await YouTubeApiClient.UploadVideoAsync(
            accessToken,
            prepared.Stream,
            fileName,
            title,
            prepared.Length,
            settings.Description,
            ParseYouTubeTags(settings.Tags),
            progress,
            cancellationToken);
    }

    static List<string> ParseYouTubeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return [];

        return tags
            .Split([',', '\n', '\r', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(t => t.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(t => t.Trim().TrimStart('#'))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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
            $"yt-upload-{Guid.NewGuid():N}{extension}");

        await using (var tempFile = File.Create(tempPath))
        {
            await videoStream.CopyToAsync(tempFile, cancellationToken);
        }

        var fileStream = File.OpenRead(tempPath);
        return new PreparedUploadStream(fileStream, fileStream.Length, disposeStream: true, tempPath);
    }

    sealed class PreparedUploadStream : IAsyncDisposable
    {
        public PreparedUploadStream(Stream stream, long length, bool disposeStream, string? tempPath)
        {
            Stream = stream;
            Length = length;
            _disposeStream = disposeStream;
            _tempPath = tempPath;
        }

        public Stream Stream { get; }
        public long Length { get; }
        private readonly bool _disposeStream;
        private readonly string? _tempPath;

        public async ValueTask DisposeAsync()
        {
            if (_disposeStream)
                await Stream.DisposeAsync();

            if (!string.IsNullOrWhiteSpace(_tempPath) && File.Exists(_tempPath))
            {
                try { File.Delete(_tempPath); }
                catch { /* best effort */ }
            }
        }
    }
}
