namespace KidsParadiseByShoptick.AdminApp.Services;

public interface IMetaVideoUploadService
{
    Task UploadAsync(
        Stream videoStream,
        string fileName,
        string title,
        string? caption = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
