using System.Globalization;
using System.Text;

namespace KidsParadiseByShoptick.AdminApp.Services;

public class MetaVideoUploadService : IMetaVideoUploadService
{
    private readonly AdminApiService _api;

    public MetaVideoUploadService(AdminApiService api) => _api = api;

    public async Task UploadAsync(
        Stream videoStream,
        string fileName,
        string title,
        decimal price,
        decimal? salePrice = null,
        string? caption = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Getting Facebook/Instagram access from server…");
        var credentials = await _api.GetMetaUploadCredentialsAsync();

        progress?.Report("Loading social media settings…");
        var settings = await _api.GetSocialMediaSettingsAsync();

        // Same caption format as photo posts (ToySocialCaptionBuilder).
        var whatsApp = string.IsNullOrWhiteSpace(credentials.WhatsAppNumber)
            ? "923217175896"
            : credentials.WhatsAppNumber;
        var postCaption = string.IsNullOrWhiteSpace(caption)
            ? BuildPhotoStyleCaption(title, price, salePrice, whatsApp, settings.Tags)
            : caption.Trim();

        await MetaVideoApiClient.UploadToFacebookAndInstagramAsync(
            credentials.FacebookPageId,
            credentials.PageAccessToken,
            credentials.InstagramBusinessAccountId,
            videoStream,
            fileName,
            contentLength: 0,
            title,
            postCaption,
            progress,
            cancellationToken);
    }

    /// <summary>Mirrors server ToySocialCaptionBuilder for video posts.</summary>
    internal static string BuildPhotoStyleCaption(
        string title,
        decimal price,
        decimal? salePrice,
        string? whatsAppNumber,
        string? tags)
    {
        var onSale = salePrice is not null && salePrice < price;
        var sb = new StringBuilder();
        sb.AppendLine(title.Trim());
        sb.AppendLine(FormatPriceLine(price, salePrice, onSale));
        sb.AppendLine("Excellent Working Condition");
        sb.AppendLine($"For price and queries please feel free to contact us on WhatsApp {FormatWhatsAppDisplay(whatsAppNumber)}");
        sb.AppendLine();

        var formattedTags = FormatTags(tags);
        if (!string.IsNullOrWhiteSpace(formattedTags))
            sb.Append(formattedTags);

        return sb.ToString().Trim();
    }

    static string FormatPriceLine(decimal price, decimal? salePrice, bool onSale)
    {
        var regular = price.ToString("N0", CultureInfo.InvariantCulture);
        if (!onSale || salePrice is null)
            return $"Price: Rs. {regular}";

        var sale = salePrice.Value.ToString("N0", CultureInfo.InvariantCulture);
        return $"Price: <del>Rs. {regular}</del> <strong>Rs. {sale}</strong>";
    }

    static string FormatWhatsAppDisplay(string? whatsAppNumber)
    {
        if (string.IsNullOrWhiteSpace(whatsAppNumber))
            return string.Empty;

        var digits = new string(whatsAppNumber.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("92", StringComparison.Ordinal) && digits.Length > 10)
            return "0" + digits[2..];
        return digits;
    }

    static string FormatTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return string.Empty;

        var parsed = tags
            .Split([',', '\n', '\r', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(t => t.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.StartsWith('#') ? t : $"#{t}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return parsed.Count == 0 ? string.Empty : string.Join(' ', parsed);
    }
}
