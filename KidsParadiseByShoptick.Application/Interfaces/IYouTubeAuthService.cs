namespace KidsParadiseByShoptick.Application.Interfaces;

public interface IYouTubeAuthService
{
    bool IsConnected { get; }
    string BuildAuthorizationUrl(out string state);
    Task CompleteAuthorizationAsync(string state, string code, CancellationToken cancellationToken = default);
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
