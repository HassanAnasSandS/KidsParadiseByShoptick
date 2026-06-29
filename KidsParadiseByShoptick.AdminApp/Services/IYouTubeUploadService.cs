namespace KidsParadiseByShoptick.AdminApp.Services;

public interface IYouTubeUploadService
{
    bool IsSupported { get; }

    Task<string> UploadAsync(
        Stream videoStream,
        string fileName,
        string title,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
