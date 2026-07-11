namespace KidsParadiseByShoptick.Application.Interfaces;

public record SocialPostJob(int ToyId, string ToyName);

public interface ISocialPostQueue
{
    ValueTask EnqueueAsync(int toyId, string toyName, CancellationToken cancellationToken = default);
}
