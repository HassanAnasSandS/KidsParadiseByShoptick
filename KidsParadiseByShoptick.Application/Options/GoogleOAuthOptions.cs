namespace KidsParadiseByShoptick.Application.Options;

public class GoogleOAuthOptions
{
    public const string SectionName = "GoogleOAuth";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string YouTubeUploadScope { get; set; } = "https://www.googleapis.com/auth/youtube.upload";
}
