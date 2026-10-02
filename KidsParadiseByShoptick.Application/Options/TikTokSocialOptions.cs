namespace KidsParadiseByShoptick.Application.Options;

public class TikTokSocialOptions
{
    public const string SectionName = "TikTokSocial";

    public bool Enabled { get; set; }
    public string ClientKey { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// MEDIA_UPLOAD = TikTok inbox draft (video.upload). DIRECT_POST = publish immediately (video.publish).
    /// Appsettings value is the default; Admin Social Settings can override at runtime.
    /// </summary>
    public string PostMode { get; set; } = DirectPost;

    /// <summary>Used only for DIRECT_POST. Prefer PUBLIC_TO_EVERYONE; must match creator_info options.</summary>
    public string PrivacyLevel { get; set; } = "PUBLIC_TO_EVERYONE";

    /// <summary>
    /// Optional override. When empty, scopes are derived from the selected PostMode.
    /// Direct Post needs video.publish. Draft uses video.upload.
    /// Requesting an unapproved scope causes TikTok's OAuth "scope" error page.
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    public const string DirectPost = "DIRECT_POST";
    public const string Draft = "MEDIA_UPLOAD";

    public static string NormalizePostMode(string? postMode) =>
        string.Equals(postMode?.Trim(), Draft, StringComparison.OrdinalIgnoreCase)
            ? Draft
            : DirectPost;

    public static bool IsDraft(string? postMode) =>
        string.Equals(NormalizePostMode(postMode), Draft, StringComparison.OrdinalIgnoreCase);

    public static string ScopesForPostMode(string? postMode) =>
        IsDraft(postMode)
            ? "user.info.basic,video.upload"
            : "user.info.basic,video.publish";
}
