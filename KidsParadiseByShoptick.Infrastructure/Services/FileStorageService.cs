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
