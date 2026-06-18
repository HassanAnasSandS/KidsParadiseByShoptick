namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveImageAsync(Stream fileStream, string fileName, string folder, CancellationToken cancellationToken = default);
    void DeleteImage(string? relativePath);
    string GetPublicUrl(string? relativePath);
}
