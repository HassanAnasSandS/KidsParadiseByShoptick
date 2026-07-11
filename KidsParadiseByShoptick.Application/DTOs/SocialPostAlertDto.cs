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
    string? Message,
    DateTimeOffset CompletedAt);
