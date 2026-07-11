using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface ISocialPostNotificationService
{
    Task NotifyAsync(int toyId, string toyName, SocialPostResultDto result, CancellationToken cancellationToken = default);
}
