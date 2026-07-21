namespace KidsParadiseByShoptick.Application.DTOs;

public record SocialPostAlertDto(
    int ToyId,
    string ToyName,
    bool FacebookPosted,
    string? FacebookPostId,
    bool InstagramPosted,
    string? InstagramPostId,
    bool WhatsAppCatalogPosted,
    string? WhatsAppCatalogProductId,
    bool TikTokPosted,
    string? TikTokPublishId,
    bool PinterestPosted,
    string? PinterestPinId,
    string? Message,
    DateTimeOffset CompletedAt);
