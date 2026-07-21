namespace KidsParadiseByShoptick.Application.Options;

public class TikTokSocialOptions
{
    public const string SectionName = "TikTokSocial";

    public bool Enabled { get; set; }
    public string ClientKey { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// MEDIA_UPLOAD = TikTok inbox draft (video.upload). DIRECT_POST = publish immediately (video.publish, audit often required).
    /// </summary>
    public string PostMode { get; set; } = "MEDIA_UPLOAD";

    /// <summary>Used only for DIRECT_POST. SELF_ONLY is safest before TikTok app audit.</summary>
    public string PrivacyLevel { get; set; } = "SELF_ONLY";

    public string Scopes { get; set; } = "user.info.basic,video.upload";
}
