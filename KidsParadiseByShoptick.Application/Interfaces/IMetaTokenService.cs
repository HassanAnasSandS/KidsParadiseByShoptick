namespace KidsParadiseByShoptick.Application.Interfaces;

public record MetaPageCredentials(
    string FacebookPageId,
    string PageAccessToken,
    string? InstagramBusinessAccountId,
    string? WhatsAppBusinessAccountId = null,
    string? WhatsAppCatalogId = null);

public interface IMetaTokenService
{
    bool IsConfigured { get; }

    Task<MetaPageCredentials> EnsureCredentialsAsync(CancellationToken cancellationToken = default);

    Task<MetaPageCredentials> ConnectAsync(MetaConnectRequest request, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task<bool> TryMaintainAsync(CancellationToken cancellationToken = default);

    /// <summary>Long-lived user token from oauth store (needed for WABA catalog link; page token often lacks access).</summary>
    string? GetStoredUserAccessToken();
}
