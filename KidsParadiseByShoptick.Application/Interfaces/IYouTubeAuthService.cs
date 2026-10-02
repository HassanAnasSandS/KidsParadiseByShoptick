namespace KidsParadiseByShoptick.Application.Interfaces;

public interface IYouTubeAuthService
{
    bool IsOAuthConfigured { get; }
    bool IsConnected { get; }
    string BuildAuthorizationUrl(out string state);
    Task CompleteAuthorizationAsync(string state, string code, CancellationToken cancellationToken = default);
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default);
}
