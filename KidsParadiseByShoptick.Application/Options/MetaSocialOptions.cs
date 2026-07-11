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
    public string WhatsAppBusinessAccountId { get; set; } = string.Empty;
    public string WhatsAppCatalogId { get; set; } = string.Empty;
    /// <summary>When false, toy posts skip Meta catalog upsert (Facebook/Instagram still run).</summary>
    public bool WhatsAppCatalogEnabled { get; set; } = true;
    public string PageAccessToken { get; set; } = string.Empty;
    public string LongLivedUserToken { get; set; } = string.Empty;
}
