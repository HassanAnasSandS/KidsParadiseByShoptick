using KidsParadiseByShoptick.AdminApp.Models;

namespace KidsParadiseByShoptick.AdminApp.Helpers;

public static class AffiliateWhatsAppHelper
{
    public static string BuildPartnerMessage(AffiliatePartnerModel partner)
    {
        var site = KidsParadiseByShoptick.AdminApp.Config.AppSettings.SiteBaseUrl.TrimEnd('/');
        var lines = new List<string>
        {
            $"Hi {partner.Name}!",
            "",
            "Here are your Kids Paradise Affiliate Partner details:",
            "",
            $"Partner: {partner.Name}",
            $"Code: {partner.Code}",
            $"Status: {(partner.IsActive ? "Active" : "Inactive")}",
            $"Your link: {site}/?aff={partner.Code}",
            $"Ledger: {site}/partner?code={partner.Code}",
            "",
            $"Orders: {partner.AttributedOrders}",
            $"Commission earned: Rs. {partner.TotalCommission:N2}",
            $"Paid: Rs. {partner.TotalPaid:N2}",
            $"Outstanding: Rs. {partner.Balance:N2}",
        };

        if (!string.IsNullOrWhiteSpace(partner.WalletBankName) || !string.IsNullOrWhiteSpace(partner.AccountNumber))
        {
            lines.Add("");
            lines.Add("Payout details:");
            if (!string.IsNullOrWhiteSpace(partner.WalletBankName))
                lines.Add($"Wallet / Bank: {partner.WalletBankName}");
            if (!string.IsNullOrWhiteSpace(partner.AccountNumber))
                lines.Add($"Account: {partner.AccountNumber}");
        }

        lines.Add("");
        lines.Add("You earn 10% commission on product amount after discount (delivery excluded).");
        lines.Add("");
        lines.Add("Thank you!");
        lines.Add("Kids Paradise by Shoptick");

        return string.Join("\n", lines);
    }

    public static string BuildLedgerMessage(AffiliateLedgerModel ledger)
    {
        var partner = ledger.Partner;
        var site = KidsParadiseByShoptick.AdminApp.Config.AppSettings.SiteBaseUrl.TrimEnd('/');
        var lines = new List<string>
        {
            $"Hi {partner.Name}!",
            "",
            "Kids Paradise — Affiliate ledger update:",
            "",
            $"Partner: {partner.Name}",
            $"Code: {partner.Code}",
            $"Your link: {site}/?aff={partner.Code}",
            "",
            $"Earned: Rs. {ledger.TotalCommission:N2}",
            $"Paid: Rs. {ledger.TotalPaid:N2}",
            $"Outstanding: Rs. {ledger.Balance:N2}",
            $"Orders: {partner.AttributedOrders}",
        };

        var recent = ledger.Entries.Take(8).ToList();
        if (recent.Count > 0)
        {
            lines.Add("");
            lines.Add("Recent entries:");
            foreach (var e in recent)
            {
                var orderBit = string.IsNullOrWhiteSpace(e.OrderNumber) ? "" : $" · Order {e.OrderNumber}";
                lines.Add($"• {e.Type}: Rs. {e.Amount:N2}{orderBit}");
            }
            if (ledger.Entries.Count > recent.Count)
                lines.Add($"…and {ledger.Entries.Count - recent.Count} more");
        }

        lines.Add("");
        lines.Add($"Ledger: {site}/partner?code={partner.Code}");
        lines.Add("");
        lines.Add("Thank you!");
        lines.Add("Kids Paradise by Shoptick");

        return string.Join("\n", lines);
    }

    public static Uri BuildChatUri(string? whatsapp, string message)
    {
        var phone = OrderWhatsAppHelper.ToWhatsAppPhone(whatsapp ?? string.Empty);
        return new Uri($"https://wa.me/{phone}?text={Uri.EscapeDataString(message)}");
    }

    public static Task OpenPartnerChatAsync(AffiliatePartnerModel partner)
    {
        if (string.IsNullOrWhiteSpace(partner.Whatsapp))
            throw new InvalidOperationException("Partner WhatsApp number is missing.");

        return Launcher.Default.OpenAsync(BuildChatUri(partner.Whatsapp, BuildPartnerMessage(partner)));
    }

    public static Task OpenLedgerChatAsync(AffiliateLedgerModel ledger)
    {
        if (string.IsNullOrWhiteSpace(ledger.Partner.Whatsapp))
            throw new InvalidOperationException("Partner WhatsApp number is missing.");

        return Launcher.Default.OpenAsync(BuildChatUri(ledger.Partner.Whatsapp, BuildLedgerMessage(ledger)));
    }

    public static Task OpenWelcomeChatAsync(AffiliatePartnerModel partner)
    {
        if (string.IsNullOrWhiteSpace(partner.Whatsapp))
            throw new InvalidOperationException("Partner WhatsApp number is missing.");

        return Launcher.Default.OpenAsync(BuildChatUri(partner.Whatsapp, partner.PartnerWelcomeMessage));
    }
}
