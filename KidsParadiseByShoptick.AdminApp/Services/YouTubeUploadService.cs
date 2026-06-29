namespace KidsParadiseByShoptick.AdminApp.Services;

public class YouTubeUploadService : IYouTubeUploadService
{
    private readonly AdminApiService _api;

    public YouTubeUploadService(AdminApiService api) => _api = api;

    public bool IsSupported => true;

    public async Task<string> UploadAsync(
        Stream videoStream,
        string fileName,
        string title,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Getting YouTube access from server…");
        var (accessToken, authUrl) = await _api.GetYouTubeAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            if (!string.IsNullOrWhiteSpace(authUrl))
            {
                await Launcher.OpenAsync(new Uri(authUrl));
                throw new InvalidOperationException(
                    "YouTube is not connected yet. Complete Google sign-in in the browser, then tap Upload again.");
            }

            throw new InvalidOperationException("Could not get YouTube access token from the server.");
        }

        return await YouTubeApiClient.UploadVideoAsync(
            accessToken,
            videoStream,
            fileName,
            title,
            progress,
            cancellationToken);
    }
}
