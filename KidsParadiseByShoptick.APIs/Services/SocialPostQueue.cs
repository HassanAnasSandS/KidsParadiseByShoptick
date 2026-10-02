using System.Threading.Channels;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;

namespace KidsParadiseByShoptick.APIs.Services;

public class SocialPostQueue : ISocialPostQueue
{
    private readonly Channel<SocialPostJob> _channel = Channel.CreateUnbounded<SocialPostJob>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<SocialPostJob> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(
        int toyId,
        string toyName,
        SocialPostTrigger trigger,
        CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(new SocialPostJob(toyId, toyName, trigger), cancellationToken);
}
