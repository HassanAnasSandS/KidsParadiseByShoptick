using System.Globalization;
using System.Text;
using KidsParadiseByShoptick.APIs.Hubs;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.APIs.Services;

public class SignalROrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<AdminOrderHub> _hub;
    private readonly SeoOptions _seoOptions;

    public SignalROrderNotificationService(IHubContext<AdminOrderHub> hub, IOptions<SeoOptions> seoOptions)
    {
        _hub = hub;
        _seoOptions = seoOptions.Value;
    }

    public async Task NotifyNewOrderAsync(NewOrderNotification notification, CancellationToken cancellationToken = default)
    {
        var body = BuildMessage(notification, AdminOrdersUrl);
        var alert = new OrderAlertDto(
            Id: Guid.NewGuid().ToString("N"),
            OrderNumber: notification.OrderNumber,
            Title: $"New Order {notification.OrderNumber}",
            Body: body,
            CustomerName: notification.CustomerName,
            Total: notification.Total,
            ReceivedAt: DateTimeOffset.UtcNow);

        await _hub.Clients.Group(AdminOrderHub.GroupName).SendAsync("NewOrder", alert, cancellationToken);
    }

    private string AdminOrdersUrl => $"{_seoOptions.SiteBaseUrl.TrimEnd('/')}/admin/orders";

    private static string BuildMessage(NewOrderNotification n, string adminOrdersUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🧸 NEW ORDER — Kids Paradise");
        sb.AppendLine();
        sb.AppendLine($"Order #: {n.OrderNumber}");
        sb.AppendLine("Status: Pending");
        sb.AppendLine();
        sb.AppendLine("— Customer —");
        sb.AppendLine($"Name: {n.CustomerName}");
        sb.AppendLine($"WhatsApp: {n.Whatsapp}");
        sb.AppendLine($"City: {n.City}");
        sb.AppendLine($"Address: {n.Address}");
        sb.AppendLine();
        sb.AppendLine("— Items —");

        for (var i = 0; i < n.Items.Count; i++)
        {
            var item = n.Items[i];
            sb.AppendLine($"{i + 1}. {item.ToyName} — {FormatRs(item.Price)}");
        }

        sb.AppendLine();
        sb.AppendLine("— Payment —");
        sb.AppendLine($"Subtotal: {FormatRs(n.SubTotal)}");
        sb.AppendLine($"Delivery: {FormatRs(n.DeliveryCharge)}");
        sb.AppendLine($"Total: {FormatRs(n.Total)}");
        sb.AppendLine();
        sb.AppendLine("10% advance required · balance on delivery");
        sb.AppendLine();
        sb.AppendLine(adminOrdersUrl);

        return sb.ToString().TrimEnd();
    }

    private static string FormatRs(decimal amount)
        => $"Rs. {amount.ToString("N0", CultureInfo.InvariantCulture)}";
}
