using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KidsParadiseByShoptick.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public FileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "KidsParadiseByShoptick.Published");
        _basePath = Path.GetFullPath(_basePath);
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveImageAsync(
        Stream fileStream, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        var safeName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var relativePath = Path.Combine("uploads", folder, safeName).Replace('\\', '/');
        var fullPath = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(stream, cancellationToken);

        return relativePath;
    }

    public async Task<string?> CopyImageAsync(string? sourceRelativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceRelativePath)) return null;

        var normalizedSource = sourceRelativePath.Replace('\\', '/').TrimStart('/');
        var sourceFull = Path.Combine(_basePath, normalizedSource.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourceFull)) return null;

        var extension = Path.GetExtension(sourceFull);
        var sourceDir = Path.GetDirectoryName(normalizedSource)!;
        var relativePath = Path.Combine(sourceDir, $"{Guid.NewGuid():N}{extension}").Replace('\\', '/');
        var destFull = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destFull)!);

        await using var sourceStream = new FileStream(sourceFull, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var destStream = new FileStream(destFull, FileMode.Create);
        await sourceStream.CopyToAsync(destStream, cancellationToken);

        return relativePath;
    }

    public void DeleteImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var fullPath = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public string GetPublicUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;
        return "/" + relativePath.TrimStart('/');
    }
}
