namespace KidsParadiseByShoptick.Application.Interfaces;

public record MetaPageCredentials(
    string FacebookPageId,
    string PageAccessToken,
    string? InstagramBusinessAccountId);

public interface IMetaTokenService
{
    bool IsConfigured { get; }

    Task<MetaPageCredentials> EnsureCredentialsAsync(CancellationToken cancellationToken = default);

    Task<MetaPageCredentials> ConnectAsync(string userAccessToken, CancellationToken cancellationToken = default);
}
