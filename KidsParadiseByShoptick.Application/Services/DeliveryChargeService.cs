using System.Text.Json;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KidsParadiseByShoptick.Application.Services;

public class DeliveryChargeService : IDeliveryChargeService
{
    public const decimal DefaultKarachi = 300m;
    public const decimal DefaultOtherCities = 400m;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _settingsFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private DeliveryChargeSettingsData _cache = CreateDefaults();
    private bool _loaded;

    public DeliveryChargeService(IConfiguration configuration)
    {
        var basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "KidsParadiseByShoptick.Published");
        basePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(Path.Combine(basePath, ".app-data"));
        _settingsFilePath = Path.Combine(basePath, ".app-data", "delivery-charges.json");
    }

    public decimal Calculate(string city)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(city))
            return _cache.OtherCities;

        return city.Trim().Equals("karachi", StringComparison.OrdinalIgnoreCase)
            ? _cache.Karachi
            : _cache.OtherCities;
    }

    public async Task<DeliveryChargeSettingsDto> GetAsync(CancellationToken cancellationToken = default)
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

    public async Task<DeliveryChargeSettingsDto> UpdateAsync(
        UpdateDeliveryChargeSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Karachi < 0 || request.OtherCities < 0)
            throw new InvalidOperationException("Delivery charges cannot be negative.");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _cache = new DeliveryChargeSettingsData
            {
                Karachi = decimal.Round(request.Karachi, 2),
                OtherCities = decimal.Round(request.OtherCities, 2),
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

    void EnsureLoaded()
    {
        if (_loaded) return;
        _lock.Wait();
        try
        {
            if (_loaded) return;
            LoadLockedAsync(CancellationToken.None).GetAwaiter().GetResult();
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
            var data = await JsonSerializer.DeserializeAsync<DeliveryChargeSettingsData>(
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

    static DeliveryChargeSettingsData CreateDefaults()
        => new() { Karachi = DefaultKarachi, OtherCities = DefaultOtherCities };

    static DeliveryChargeSettingsData Normalize(DeliveryChargeSettingsData? data)
    {
        if (data is null) return CreateDefaults();
        return new DeliveryChargeSettingsData
        {
            Karachi = data.Karachi >= 0 ? data.Karachi : DefaultKarachi,
            OtherCities = data.OtherCities >= 0 ? data.OtherCities : DefaultOtherCities,
        };
    }

    static DeliveryChargeSettingsDto Map(DeliveryChargeSettingsData data)
        => new(data.Karachi, data.OtherCities);

    private sealed class DeliveryChargeSettingsData
    {
        public decimal Karachi { get; set; } = DefaultKarachi;
        public decimal OtherCities { get; set; } = DefaultOtherCities;
    }
}
