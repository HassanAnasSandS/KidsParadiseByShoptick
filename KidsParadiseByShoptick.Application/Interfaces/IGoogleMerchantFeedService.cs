namespace KidsParadiseByShoptick.Application.Interfaces;

public interface IGoogleMerchantFeedService
{
    Task<string> GenerateFeedXmlAsync(string baseUrl, CancellationToken cancellationToken = default);
}

public interface IGoogleMerchantFeedCache
{
    long Version { get; }
    void Invalidate();
}
