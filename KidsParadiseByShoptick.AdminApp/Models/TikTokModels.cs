using System.Text.Json.Serialization;

namespace KidsParadiseByShoptick.AdminApp.Models;

public class TikTokStatusModel
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("configured")] public bool Configured { get; set; }
    [JsonPropertyName("connected")] public bool Connected { get; set; }
    [JsonPropertyName("postMode")] public string PostMode { get; set; } = "DIRECT_POST";
    [JsonPropertyName("privacyLevel")] public string PrivacyLevel { get; set; } = "PUBLIC_TO_EVERYONE";
    [JsonPropertyName("needsReconnect")] public bool NeedsReconnect { get; set; }
}

public class TikTokAuthUrlModel
{
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
}

public class TikTokAccessTokenModel
{
    [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("openId")] public string? OpenId { get; set; }
    [JsonPropertyName("postMode")] public string PostMode { get; set; } = "DIRECT_POST";
    [JsonPropertyName("privacyLevel")] public string PrivacyLevel { get; set; } = "PUBLIC_TO_EVERYONE";
    [JsonPropertyName("needsAuth")] public bool NeedsAuth { get; set; }
    [JsonPropertyName("authUrl")] public string? AuthUrl { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}
