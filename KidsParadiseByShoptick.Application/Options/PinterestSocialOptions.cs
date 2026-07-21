namespace KidsParadiseByShoptick.Application.Options;

public class PinterestSocialOptions
{
    public const string SectionName = "PinterestSocial";

    public bool Enabled { get; set; }

    /// <summary>Pinterest app ID (client id).</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Pinterest app secret.</summary>
    public string AppSecret { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Optional fixed board. If empty, the service uses/creates "Kids Paradise Toys".
    /// </summary>
    public string BoardId { get; set; } = string.Empty;

    public string DefaultBoardName { get; set; } = "Kids Paradise Toys";

    public string Scopes { get; set; } = "boards:read,boards:write,pins:write,user_accounts:read";
}
