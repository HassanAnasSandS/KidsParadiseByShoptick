using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ISocialMediaService
{
    Task<SocialPostResultDto> PostToyAsync(int toyId, CancellationToken cancellationToken = default);
}
