using System.Globalization;
using System.Text;
using KidsParadiseByShoptick.Application.Helpers;
using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Application.Services;

internal static class ToySocialCaptionBuilder
{
    public static string Build(Toy toy, string siteBaseUrl, string whatsAppNumber, string? tags = null)
    {
        var onSale = toy.SalePrice is not null && toy.SalePrice < toy.Price;

        var sb = new StringBuilder();
        sb.AppendLine(toy.Name.Trim());
        sb.AppendLine(FormatPriceLine(toy, onSale));
        sb.AppendLine("Excellent Working Condition");
        sb.AppendLine($"For price and queries please feel free to contact us on WhatsApp {FormatWhatsAppDisplay(whatsAppNumber)}");
        sb.AppendLine();

        var formattedTags = SocialMediaTagHelper.FormatTagsForCaption(tags);
        if (!string.IsNullOrWhiteSpace(formattedTags))
            sb.Append(formattedTags);

        return sb.ToString().Trim();
    }

    static string FormatPriceLine(Toy toy, bool onSale)
    {
        var regular = toy.Price.ToString("N0", CultureInfo.InvariantCulture);
        if (!onSale)
            return $"Price: Rs. {regular}";

        var sale = toy.SalePrice!.Value.ToString("N0", CultureInfo.InvariantCulture);
        return $"Price: <del>Rs. {regular}</del> <strong>Rs. {sale}</strong>";
    }

    static string FormatWhatsAppDisplay(string whatsAppNumber)
    {
        var digits = new string(whatsAppNumber.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("92", StringComparison.Ordinal) && digits.Length > 10)
            return "0" + digits[2..];
        return digits;
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
