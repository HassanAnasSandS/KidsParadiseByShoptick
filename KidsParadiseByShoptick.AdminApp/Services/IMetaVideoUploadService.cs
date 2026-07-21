namespace KidsParadiseByShoptick.AdminApp.Services;

public interface IMetaVideoUploadService
{
    Task UploadAsync(
        Stream videoStream,
        string fileName,
        string title,
        decimal price,
        decimal? salePrice = null,
        string? caption = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
