using System.Globalization;
using System.Text;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Infrastructure.Services;

public class NtfyOrderNotificationService : IOrderNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly NtfyOptions _options;
    private readonly SeoOptions _seoOptions;
    private readonly ILogger<NtfyOrderNotificationService> _logger;

    public NtfyOrderNotificationService(
        HttpClient httpClient,
        IOptions<NtfyOptions> options,
        IOptions<SeoOptions> seoOptions,
        ILogger<NtfyOrderNotificationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _seoOptions = seoOptions.Value;
        _logger = logger;
    }

    public async Task NotifyNewOrderAsync(NewOrderNotification notification, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.TopicUrl))
            return;

        var message = BuildMessage(notification, AdminOrdersUrl);
        var request = new HttpRequestMessage(HttpMethod.Post, _options.TopicUrl.Trim())
        {
            Content = new StringContent(message, Encoding.UTF8, "text/plain")
        };

        request.Headers.TryAddWithoutValidation("Title", $"New Order {notification.OrderNumber}");
        request.Headers.TryAddWithoutValidation("Tags", "shopping_cart,money_with_wings");
        request.Headers.TryAddWithoutValidation("Priority", "high");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("ntfy notification failed ({Status}): {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send order notification to ntfy");
        }
    }

    private string AdminOrdersUrl
        => $"{_seoOptions.SiteBaseUrl.TrimEnd('/')}/admin/orders";

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
