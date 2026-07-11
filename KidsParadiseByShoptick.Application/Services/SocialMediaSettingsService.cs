using System.Text.Json;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KidsParadiseByShoptick.Application.Services;

public class SocialMediaSettingsService : ISocialMediaSettingsService
{
    public const string DefaultDescription =
        "For Order Whatsapp 0321-7175-896 Or Visit https://kidsparadise.shoptick.shop/";

    public const string DefaultTags = "#KidsParadise #Toys #Karachi #Pakistan";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _settingsFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SocialMediaSettingsService(IConfiguration configuration)
    {
        var basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "KidsParadiseByShoptick.Published");
        basePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(Path.Combine(basePath, ".app-data"));
        _settingsFilePath = Path.Combine(basePath, ".app-data", "social-media-settings.json");
    }

    public async Task<SocialMediaSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return MapToDto(await LoadAsync(cancellationToken));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<SocialMediaSettingsDto> UpdateAsync(
        UpdateSocialMediaSettingsRequest request, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var data = new SocialMediaSettingsData
            {
                Description = request.Description?.Trim() ?? string.Empty,
                Tags = request.Tags?.Trim() ?? string.Empty,
            };

            await SaveAsync(data, cancellationToken);
            return MapToDto(data);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<SocialMediaSettingsData> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsFilePath))
            return CreateDefaults();

        try
        {
            await using var stream = File.OpenRead(_settingsFilePath);
            var data = await JsonSerializer.DeserializeAsync<SocialMediaSettingsData>(stream, JsonOptions, cancellationToken);
            return data ?? CreateDefaults();
        }
        catch
        {
            return CreateDefaults();
        }
    }

    private async Task SaveAsync(SocialMediaSettingsData data, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions, cancellationToken);
    }

    private static SocialMediaSettingsData CreateDefaults() =>
        new() { Description = DefaultDescription, Tags = DefaultTags };

    private static SocialMediaSettingsDto MapToDto(SocialMediaSettingsData data) =>
        new(
            string.IsNullOrWhiteSpace(data.Description) ? DefaultDescription : data.Description.Trim(),
            string.IsNullOrWhiteSpace(data.Tags) ? DefaultTags : data.Tags.Trim());

    private sealed class SocialMediaSettingsData
    {
        public string Description { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
    }
}
