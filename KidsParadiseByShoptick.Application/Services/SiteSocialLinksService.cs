using System.Text.Json;
using System.Text.RegularExpressions;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KidsParadiseByShoptick.Application.Services;

public class SiteSocialLinksService : ISiteSocialLinksService
{
    public const string DefaultWhatsAppNumber = "923217175896";
    public const string DefaultWhatsAppDisplay = "0321 7175896";
    public const string DefaultYouTubeUrl = "https://www.youtube.com/@KidsParadiseByShoptick";
    public const string DefaultFacebookUrl = "https://www.facebook.com/share/1DesofLX6U/";
    public const string DefaultInstagramUrl = "https://www.instagram.com/miniclosetpk?igsh=cnVuazFjZjE3d3Vo";
    public const string DefaultTikTokUrl = "https://www.tiktok.com/@kiranhassanhk?_r=1&_t=ZS-92ITfkXq7AG";
    public const string DefaultPinterestUrl = "";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Regex DigitsOnly = new(@"\D+", RegexOptions.Compiled);

    private readonly string _settingsFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private SiteSocialLinksData _cache = CreateDefaults();
    private bool _loaded;

    public SiteSocialLinksService(IConfiguration configuration)
    {
        var basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "KidsParadiseByShoptick.Published");
        basePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(Path.Combine(basePath, ".app-data"));
        _settingsFilePath = Path.Combine(basePath, ".app-data", "site-social-links.json");
    }

    public async Task<SiteSocialLinksDto> GetAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await LoadLockedAsync(cancellationToken);
            return Map(_cache);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<SiteSocialLinksDto> UpdateAsync(
        UpdateSiteSocialLinksRequest request,
        CancellationToken cancellationToken = default)
    {
        var number = NormalizeWhatsAppNumber(request.WhatsAppNumber);
        if (string.IsNullOrWhiteSpace(number))
            throw new InvalidOperationException("WhatsApp number is required (digits only, with country code).");

        var display = string.IsNullOrWhiteSpace(request.WhatsAppDisplay)
            ? FormatDisplayFromNumber(number)
            : request.WhatsAppDisplay.Trim();

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _cache = new SiteSocialLinksData
            {
                WhatsAppNumber = number,
                WhatsAppDisplay = display,
                YouTubeUrl = NormalizeUrl(request.YouTubeUrl),
                FacebookUrl = NormalizeUrl(request.FacebookUrl),
                InstagramUrl = NormalizeUrl(request.InstagramUrl),
                TikTokUrl = NormalizeUrl(request.TikTokUrl),
                PinterestUrl = NormalizeUrl(request.PinterestUrl),
            };
            await SaveLockedAsync(cancellationToken);
            _loaded = true;
            return Map(_cache);
        }
        finally
        {
            _lock.Release();
        }
    }

    async Task LoadLockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsFilePath))
        {
            _cache = CreateDefaults();
            _loaded = true;
            return;
        }

        try
        {
            await using var stream = File.OpenRead(_settingsFilePath);
            var data = await JsonSerializer.DeserializeAsync<SiteSocialLinksData>(
                stream, JsonOptions, cancellationToken);
            _cache = Normalize(data);
        }
        catch
        {
            _cache = CreateDefaults();
        }

        _loaded = true;
    }

    async Task SaveLockedAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, _cache, JsonOptions, cancellationToken);
    }

    static SiteSocialLinksData CreateDefaults()
        => new()
        {
            WhatsAppNumber = DefaultWhatsAppNumber,
            WhatsAppDisplay = DefaultWhatsAppDisplay,
            YouTubeUrl = DefaultYouTubeUrl,
            FacebookUrl = DefaultFacebookUrl,
            InstagramUrl = DefaultInstagramUrl,
            TikTokUrl = DefaultTikTokUrl,
            PinterestUrl = DefaultPinterestUrl,
        };

    static SiteSocialLinksData Normalize(SiteSocialLinksData? data)
    {
        if (data is null) return CreateDefaults();
        var number = NormalizeWhatsAppNumber(data.WhatsAppNumber);
        if (string.IsNullOrWhiteSpace(number))
            number = DefaultWhatsAppNumber;

        return new SiteSocialLinksData
        {
            WhatsAppNumber = number,
            WhatsAppDisplay = string.IsNullOrWhiteSpace(data.WhatsAppDisplay)
                ? FormatDisplayFromNumber(number)
                : data.WhatsAppDisplay.Trim(),
            YouTubeUrl = NormalizeUrl(data.YouTubeUrl),
            FacebookUrl = NormalizeUrl(data.FacebookUrl),
            InstagramUrl = NormalizeUrl(data.InstagramUrl),
            TikTokUrl = NormalizeUrl(data.TikTokUrl),
            PinterestUrl = NormalizeUrl(data.PinterestUrl),
        };
    }

    static string NormalizeWhatsAppNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var digits = DigitsOnly.Replace(value, string.Empty);
        if (digits.StartsWith('0') && digits.Length == 11)
            digits = "92" + digits[1..];
        return digits;
    }

    static string FormatDisplayFromNumber(string number)
    {
        if (number.StartsWith("92") && number.Length == 12)
            return $"0{number[2..5]} {number[5..]}";
        return number;
    }

    static string NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var url = value.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;
        return Uri.TryCreate(url, UriKind.Absolute, out _) ? url : string.Empty;
    }

    static SiteSocialLinksDto Map(SiteSocialLinksData data)
        => new(
            data.WhatsAppNumber,
            data.WhatsAppDisplay,
            data.YouTubeUrl,
            data.FacebookUrl,
            data.InstagramUrl,
            data.TikTokUrl,
            data.PinterestUrl);

    private sealed class SiteSocialLinksData
    {
        public string WhatsAppNumber { get; set; } = DefaultWhatsAppNumber;
        public string WhatsAppDisplay { get; set; } = DefaultWhatsAppDisplay;
        public string YouTubeUrl { get; set; } = DefaultYouTubeUrl;
        public string FacebookUrl { get; set; } = DefaultFacebookUrl;
        public string InstagramUrl { get; set; } = DefaultInstagramUrl;
        public string TikTokUrl { get; set; } = DefaultTikTokUrl;
        public string PinterestUrl { get; set; } = DefaultPinterestUrl;
    }
}
