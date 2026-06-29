using System.Globalization;
using System.Text;
using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Application.Services;

internal static class ToySocialCaptionBuilder
{
    public static string Build(Toy toy, string siteBaseUrl, string whatsAppNumber)
    {
        siteBaseUrl = siteBaseUrl.TrimEnd('/');
        var effectivePrice = toy.SalePrice is not null && toy.SalePrice < toy.Price
            ? toy.SalePrice.Value
            : toy.Price;
        var onSale = toy.SalePrice is not null && toy.SalePrice < toy.Price;

        var sb = new StringBuilder();
        sb.AppendLine($"🧸 {toy.Name.Trim()}");
        sb.AppendLine();

        if (onSale)
        {
            sb.AppendLine($"💰 Price: Rs. {effectivePrice.ToString("N0", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"   Regular: Rs. {toy.Price.ToString("N0", CultureInfo.InvariantCulture)}");
        }
        else
        {
            sb.AppendLine($"💰 Price: Rs. {effectivePrice.ToString("N0", CultureInfo.InvariantCulture)}");
        }

        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(toy.VideoLink))
            sb.AppendLine($"🎬 Video: {toy.VideoLink.Trim()}");

        sb.AppendLine($"🛒 {siteBaseUrl}/product/{toy.Id}");
        sb.AppendLine($"🌐 {siteBaseUrl}");
        sb.AppendLine($"📱 WhatsApp: https://wa.me/{whatsAppNumber}");
        sb.AppendLine();
        sb.Append("#KidsParadise #Toys #Karachi #Pakistan");

        return sb.ToString().Trim();
    }

    public static IReadOnlyList<string> BuildAbsoluteImageUrls(
        Toy toy, string siteBaseUrl, Func<string?, string> resolveRelativeUrl)
    {
        siteBaseUrl = siteBaseUrl.TrimEnd('/');
        return toy.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => i.ImagePath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p =>
            {
                var url = resolveRelativeUrl(p);
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return url;
                return siteBaseUrl + (url.StartsWith('/') ? url : "/" + url);
            })
            .Distinct()
            .ToList();
    }
}
