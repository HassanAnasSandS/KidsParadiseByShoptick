using KidsParadiseByShoptick.AdminApp.Models;

namespace KidsParadiseByShoptick.AdminApp.Helpers;

public static class OrderWhatsAppHelper
{
    public static string ToWhatsAppPhone(string whatsapp)
    {
        var digits = new string(whatsapp.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return digits;

        if (digits.StartsWith("92", StringComparison.Ordinal)) return digits;
        if (digits.StartsWith('0') && digits.Length >= 10) return "92" + digits[1..];
        if (digits.Length == 10) return "92" + digits;
        return digits;
    }

    public static string BuildCustomerMessage(OrderModel order)
    {
        var discount = order.DiscountAmount ?? 0;
        var advance = order.AdvanceAmount ?? 0;
        var showPayment = order.Status is "Confirmed" or "Shipped" or "Delivered";

        var items = string.Join("\n", order.Items.Select((item, i) =>
            $"{i + 1}. {item.ToyName}\n   Rs. {item.Price:N0}"));

        var lines = new List<string>
        {
            $"Hello {order.CustomerName}!",
            "",
            "Here are your order details from Kids Paradise:",
            "",
            $"📦 Order: {order.OrderNumber}",
            $"📋 Status: {order.Status}",
            $"🏙️ City: {order.City}",
            $"📍 Address: {order.Address}",
            "",
            "🛍️ Items:",
            items,
            "",
            $"Products Subtotal: Rs. {order.SubTotal:N0}",
            $"Delivery Charges: Rs. {order.DeliveryCharge:N0}",
            $"Order Total: Rs. {order.Total:N0}",
        };

        if (discount > 0) lines.Add($"Discount: -Rs. {discount:N0}");
        if (showPayment && advance > 0) lines.Add($"Advance Paid: Rs. {advance:N0}");
        if (showPayment) lines.Add($"Balance Due: Rs. {order.BalanceAmount:N0}");
        if (!string.IsNullOrWhiteSpace(order.TrackingNumber))
            lines.Add($"Tracking: {order.TrackingNumber}");

        lines.Add("");
        lines.Add("Thank you for shopping with Kids Paradise!");

        return string.Join("\n", lines);
    }

    public static Uri BuildCustomerChatUri(OrderModel order)
    {
        var phone = ToWhatsAppPhone(order.Whatsapp);
        var message = BuildCustomerMessage(order);
        return new Uri($"https://wa.me/{phone}?text={Uri.EscapeDataString(message)}");
    }

    public static Task OpenCustomerChatAsync(OrderModel order) =>
        Launcher.Default.OpenAsync(BuildCustomerChatUri(order));
}
