using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ISocialMediaSettingsService
{
    Task<SocialMediaSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<SocialMediaSettingsDto> UpdateAsync(UpdateSocialMediaSettingsRequest request, CancellationToken cancellationToken = default);
}
