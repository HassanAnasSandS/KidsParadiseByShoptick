namespace KidsParadiseByShoptick.Application.Options;

public class MetaSocialOptions
{
    public const string SectionName = "MetaSocial";

    public bool Enabled { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string SiteBaseUrl { get; set; } = "https://kidsparadise.shoptick.shop";
    public string WhatsAppNumber { get; set; } = "923217175896";
    public string FacebookPageId { get; set; } = string.Empty;
    public string InstagramBusinessAccountId { get; set; } = string.Empty;
    public string PageAccessToken { get; set; } = string.Empty;
    public string LongLivedUserToken { get; set; } = string.Empty;
}
