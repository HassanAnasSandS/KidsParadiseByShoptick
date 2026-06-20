namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ISitemapService
{
    Task<string> GenerateSitemapXmlAsync(string baseUrl, CancellationToken cancellationToken = default);
    string GenerateRobotsTxt(string baseUrl);
}
