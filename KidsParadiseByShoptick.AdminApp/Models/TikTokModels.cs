using System.Text.Json.Serialization;

namespace KidsParadiseByShoptick.AdminApp.Models;

public class TikTokStatusModel
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("configured")] public bool Configured { get; set; }
    [JsonPropertyName("connected")] public bool Connected { get; set; }
    [JsonPropertyName("postMode")] public string PostMode { get; set; } = "MEDIA_UPLOAD";
    [JsonPropertyName("privacyLevel")] public string PrivacyLevel { get; set; } = "SELF_ONLY";
}

public class TikTokAuthUrlModel
{
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
}

public class TikTokAccessTokenModel
{
    [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("openId")] public string? OpenId { get; set; }
    [JsonPropertyName("postMode")] public string PostMode { get; set; } = "MEDIA_UPLOAD";
    [JsonPropertyName("privacyLevel")] public string PrivacyLevel { get; set; } = "SELF_ONLY";
    [JsonPropertyName("needsAuth")] public bool NeedsAuth { get; set; }
    [JsonPropertyName("authUrl")] public string? AuthUrl { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}
