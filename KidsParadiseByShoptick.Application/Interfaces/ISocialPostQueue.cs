using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public record SocialPostJob(int ToyId, string ToyName, SocialPostTrigger Trigger);

public interface ISocialPostQueue
{
    ValueTask EnqueueAsync(
        int toyId,
        string toyName,
        SocialPostTrigger trigger,
        CancellationToken cancellationToken = default);
}
