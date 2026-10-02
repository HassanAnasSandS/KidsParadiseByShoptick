using System.Text.Json;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class SocialMediaSettingsService : ISocialMediaSettingsService
{
    public const string DefaultDescription =
        "For Order Whatsapp 0321-7175-896 Or Visit https://kidsparadise.shoptick.shop/";

    public const string DefaultTags = "#KidsParadise #Toys #Karachi #Pakistan";
    // Keep ≤5 hashtags so TikTok posts stay within platform limits when defaults are used.

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _settingsFilePath;
    private readonly TikTokSocialOptions _tikTokOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SocialMediaSettingsService(
        IConfiguration configuration,
        IOptions<TikTokSocialOptions> tikTokOptions)
    {
        _tikTokOptions = tikTokOptions.Value;
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

    public async Task<string> GetTikTokPostModeAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return ResolvePostMode(await LoadAsync(cancellationToken));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> SetTikTokPostModeAsync(string postMode, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var data = await LoadAsync(cancellationToken);
            data.TikTokPostMode = TikTokSocialOptions.NormalizePostMode(postMode);
            await SaveAsync(data, cancellationToken);
            return data.TikTokPostMode;
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
            var existing = await LoadAsync(cancellationToken);
            existing.Description = request.Description?.Trim() ?? string.Empty;
            existing.Tags = request.Tags?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(request.TikTokPostMode))
                existing.TikTokPostMode = TikTokSocialOptions.NormalizePostMode(request.TikTokPostMode);
            if (request.OnCreate is not null)
                existing.OnCreate = CloneActions(request.OnCreate);
            if (request.OnEdit is not null)
                existing.OnEdit = CloneActions(request.OnEdit);

            await SaveAsync(existing, cancellationToken);
            return MapToDto(existing);
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

    private SocialMediaSettingsData CreateDefaults() =>
        new()
        {
            Description = DefaultDescription,
            Tags = DefaultTags,
            TikTokPostMode = TikTokSocialOptions.NormalizePostMode(_tikTokOptions.PostMode),
            OnCreate = CloneActions(SocialPostActionsDto.AllEnabled),
            OnEdit = CloneActions(SocialPostActionsDto.AllEnabled),
        };

    private SocialMediaSettingsDto MapToDto(SocialMediaSettingsData data) =>
        new(
            string.IsNullOrWhiteSpace(data.Description) ? DefaultDescription : data.Description.Trim(),
            string.IsNullOrWhiteSpace(data.Tags) ? DefaultTags : data.Tags.Trim(),
            ResolvePostMode(data),
            NormalizeActions(data.OnCreate),
            NormalizeActions(data.OnEdit));

    private string ResolvePostMode(SocialMediaSettingsData data) =>
        TikTokSocialOptions.NormalizePostMode(
            string.IsNullOrWhiteSpace(data.TikTokPostMode) ? _tikTokOptions.PostMode : data.TikTokPostMode);

    private static SocialPostActionsDto NormalizeActions(SocialPostActionsData? data) =>
        data is null
            ? SocialPostActionsDto.AllEnabled
            : new SocialPostActionsDto(
                data.FacebookPhotos,
                data.InstagramPhotos,
                data.WhatsAppCatalog,
                data.TikTokPhotos,
                data.Pinterest,
                data.YouTube,
                data.MetaVideo,
                data.TikTokVideo);

    private static SocialPostActionsData CloneActions(SocialPostActionsDto dto) =>
        new()
        {
            FacebookPhotos = dto.FacebookPhotos,
            InstagramPhotos = dto.InstagramPhotos,
            WhatsAppCatalog = dto.WhatsAppCatalog,
            TikTokPhotos = dto.TikTokPhotos,
            Pinterest = dto.Pinterest,
            YouTube = dto.YouTube,
            MetaVideo = dto.MetaVideo,
            TikTokVideo = dto.TikTokVideo,
        };

    private sealed class SocialMediaSettingsData
    {
        public string Description { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string TikTokPostMode { get; set; } = string.Empty;
        public SocialPostActionsData? OnCreate { get; set; }
        public SocialPostActionsData? OnEdit { get; set; }
    }

    private sealed class SocialPostActionsData
    {
        public bool FacebookPhotos { get; set; } = true;
        public bool InstagramPhotos { get; set; } = true;
        public bool WhatsAppCatalog { get; set; } = true;
        public bool TikTokPhotos { get; set; } = true;
        public bool Pinterest { get; set; } = true;
        public bool YouTube { get; set; } = true;
        public bool MetaVideo { get; set; } = true;
        public bool TikTokVideo { get; set; } = true;
    }
}
