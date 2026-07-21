using KidsParadiseByShoptick.Application.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

/// <summary>Bumps a version so MemoryCache entries for the Merchant feed are skipped after toy changes.</summary>
public sealed class GoogleMerchantFeedCache : IGoogleMerchantFeedCache
{
    private long _version;

    public long Version => Interlocked.Read(ref _version);

    public void Invalidate() => Interlocked.Increment(ref _version);
}
