using KidsParadiseByShoptick.APIs.Hubs;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace KidsParadiseByShoptick.APIs.Services;

public class SignalRSocialPostNotificationService : ISocialPostNotificationService
{
    private readonly IHubContext<AdminOrderHub> _hub;

    public SignalRSocialPostNotificationService(IHubContext<AdminOrderHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyAsync(int toyId, string toyName, SocialPostResultDto result, CancellationToken cancellationToken = default)
    {
        var alert = new SocialPostAlertDto(
            toyId,
            toyName,
            result.FacebookPosted,
            result.FacebookPostId,
            result.InstagramPosted,
            result.InstagramPostId,
            result.WhatsAppCatalogPosted,
            result.WhatsAppCatalogProductId,
            result.TikTokPosted,
            result.TikTokPublishId,
            result.PinterestPosted,
            result.PinterestPinId,
            result.Message,
            DateTimeOffset.UtcNow);

        return _hub.Clients.Group(AdminOrderHub.GroupName).SendAsync("SocialPostComplete", alert, cancellationToken);
    }
}
