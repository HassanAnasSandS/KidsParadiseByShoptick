namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ITikTokAuthService
{
    bool IsOAuthConfigured { get; }
    bool IsConnected { get; }

    string BuildAuthorizationUrl(string? postMode, out string state);

    Task CompleteAuthorizationAsync(string state, string code, CancellationToken cancellationToken = default);

    Task<(string AccessToken, string OpenId)> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
