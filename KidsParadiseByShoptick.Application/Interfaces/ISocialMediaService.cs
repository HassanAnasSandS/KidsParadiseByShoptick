using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ISocialMediaService
{
    Task<SocialPostResultDto> PostToyAsync(
        int toyId,
        SocialPostTrigger trigger = SocialPostTrigger.Create,
        CancellationToken cancellationToken = default);
}
