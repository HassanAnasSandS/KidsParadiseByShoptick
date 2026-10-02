namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ITikTokSocialService
{
    /// <summary>Client key/secret enabled on server.</summary>
    bool IsOAuthConfigured { get; }

    /// <summary>OAuth configured and a TikTok account is connected.</summary>
    bool IsConfigured { get; }

    /// <summary>Posts toy photos to TikTok (separate from Facebook/Instagram). Returns publish_id.</summary>
    Task<string?> PostToyPhotosAsync(int toyId, CancellationToken cancellationToken = default);

    /// <summary>Resolves a creator-allowed privacy_level for DIRECT_POST.</summary>
    Task<string> ResolvePrivacyLevelAsync(string accessToken, CancellationToken cancellationToken = default);
}
