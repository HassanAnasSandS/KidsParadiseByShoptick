using System.Text.Json.Serialization;

namespace KidsParadiseByShoptick.AdminApp.Models;

public class YouTubeAccessTokenResponse
{
    [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty;
}

public class YouTubeAuthRequiredResponse
{
    [JsonPropertyName("needsAuth")] public bool NeedsAuth { get; set; }
    [JsonPropertyName("authUrl")] public string? AuthUrl { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}
