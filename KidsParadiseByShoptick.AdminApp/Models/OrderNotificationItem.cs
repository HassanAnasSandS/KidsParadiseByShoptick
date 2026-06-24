namespace KidsParadiseByShoptick.AdminApp.Models;

public class OrderNotificationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }

    public string TimeLabel => ReceivedAt.ToString("dd MMM, hh:mm tt");
    public string Preview => Body.Length > 120 ? Body[..120] + "…" : Body;
}
