using System.Text.Json.Serialization;

namespace KidsParadiseByShoptick.AdminApp.Models;

public class PinterestStatusModel
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("configured")] public bool Configured { get; set; }
    [JsonPropertyName("connected")] public bool Connected { get; set; }
    [JsonPropertyName("boardId")] public string? BoardId { get; set; }
    [JsonPropertyName("defaultBoardName")] public string? DefaultBoardName { get; set; }
}

public class PinterestAuthUrlModel
{
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
}
