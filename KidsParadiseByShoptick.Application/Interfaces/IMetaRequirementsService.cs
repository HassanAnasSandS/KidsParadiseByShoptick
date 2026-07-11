using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface IMetaRequirementsService
{
    Task<MetaRequirementsStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<MetaRequirementsStatusDto> LinkCatalogAsync(
        string catalogId, CancellationToken cancellationToken = default);
}
