namespace KidsParadiseByShoptick.Application.Interfaces;

public record MetaConnectRequest(
    string UserAccessToken,
    string? FacebookPageId = null,
    string? InstagramBusinessAccountId = null,
    string? PageAccessToken = null,
    string? WhatsAppBusinessAccountId = null,
    string? WhatsAppCatalogId = null);
