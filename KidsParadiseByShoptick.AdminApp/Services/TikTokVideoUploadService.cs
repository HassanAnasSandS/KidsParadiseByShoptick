namespace KidsParadiseByShoptick.AdminApp.Services;

public class TikTokVideoUploadService : ITikTokVideoUploadService
{
    private readonly AdminApiService _api;

    public TikTokVideoUploadService(AdminApiService api) => _api = api;

    public async Task<string> UploadAsync(
        Stream videoStream,
        string fileName,
        string title,
        decimal price,
        decimal? salePrice = null,
        string? caption = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Getting TikTok access from server…");
        var creds = await _api.GetTikTokAccessTokenAsync();
        if (creds is null || string.IsNullOrWhiteSpace(creds.AccessToken))
        {
            if (!string.IsNullOrWhiteSpace(creds?.AuthUrl))
            {
                await Launcher.OpenAsync(new Uri(creds.AuthUrl));
                throw new InvalidOperationException(
                    "TikTok is not connected yet. Complete TikTok sign-in in the browser, then Save again.");
            }

            throw new InvalidOperationException("Could not get TikTok access token from the server.");
        }

        progress?.Report("Loading social media settings…");
        var settings = await _api.GetSocialMediaSettingsAsync();
        string? whatsApp = null;
        try
        {
            var metaCreds = await _api.GetMetaUploadCredentialsAsync();
            whatsApp = metaCreds.WhatsAppNumber;
        }
        catch
        {
            // Meta optional for TikTok caption WhatsApp line
        }

        var postCaption = string.IsNullOrWhiteSpace(caption)
            ? MetaVideoUploadService.BuildPhotoStyleCaption(
                title, price, salePrice, whatsApp, settings.Tags)
            : caption.Trim();

        await using var prepared = await PrepareUploadStreamAsync(videoStream, fileName, cancellationToken);
        return await TikTokApiClient.UploadVideoAsync(
            creds.AccessToken,
            creds.PostMode,
            creds.PrivacyLevel,
            prepared.Stream,
            fileName,
            prepared.Length,
            title,
            postCaption,
            progress,
            cancellationToken);
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
            $"tiktok-upload-{Guid.NewGuid():N}{extension}");

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
