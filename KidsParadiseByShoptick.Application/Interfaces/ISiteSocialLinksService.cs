using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ISiteSocialLinksService
{
    Task<SiteSocialLinksDto> GetAsync(CancellationToken cancellationToken = default);

    Task<SiteSocialLinksDto> UpdateAsync(
        UpdateSiteSocialLinksRequest request,
        CancellationToken cancellationToken = default);
}
