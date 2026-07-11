namespace KidsParadiseByShoptick.Application.DTOs;

public record MetaRequirementCheckDto(
    string Id,
    int Step,
    string Title,
    bool IsMet,
    string Status,
    string FixInstructions);

public record MetaRequirementsStatusDto(
    bool AllMet,
    string? FacebookPageId,
    string? WhatsAppBusinessAccountId,
    string? WhatsAppCatalogId,
    IReadOnlyList<MetaRequirementCheckDto> Requirements);
