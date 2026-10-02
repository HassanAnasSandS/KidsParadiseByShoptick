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
            ? BuildPhotoStyleCaption(title, price, salePrice, whatsApp, settings.Tags, plainText: true)
            : caption.Trim();

        await MetaVideoApiClient.UploadToFacebookAndInstagramAsync(
            credentials.FacebookPageId,
            credentials.PageAccessToken,
            credentials.InstagramBusinessAccountId,
            videoStream,
            fileName,
            contentLength: 0,
            title.Trim(),
            postCaption,
            progress,
            cancellationToken);
    }

    /// <summary>Mirrors server ToySocialCaptionBuilder for video posts.</summary>
    internal static string BuildPhotoStyleCaption(
        string title,
        decimal price,
        decimal? salePrice = null,
        string? whatsAppNumber = null,
        string? tags = null,
        bool plainText = false,
        int? maxHashtags = null)
    {
        var onSale = salePrice is not null && salePrice < price;
        var sb = new StringBuilder();
        sb.AppendLine(title.Trim());
        sb.AppendLine(FormatPriceLine(price, salePrice, onSale, plainText));
        sb.AppendLine("Excellent Working Condition");
        sb.AppendLine($"For price and queries please feel free to contact us on WhatsApp {FormatWhatsAppDisplay(whatsAppNumber)}");
        sb.AppendLine();

        var formattedTags = FormatTags(tags, maxHashtags);
        if (!string.IsNullOrWhiteSpace(formattedTags))
            sb.Append(formattedTags);

        return sb.ToString().Trim();
    }

    /// <summary>Mirrors server SocialMediaTagHelper.LimitHashtagsInCaption (TikTok max ~5).</summary>
    internal static string LimitHashtagsInCaption(string? caption, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(caption) || maxCount <= 0)
            return caption?.Trim() ?? string.Empty;

        var lines = caption.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var keptHashtags = 0;
        var output = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                output.Add(string.Empty);
                continue;
            }

            var rebuilt = new List<string>(parts.Length);
            foreach (var part in parts)
            {
                if (part.StartsWith('#') && part.Length > 1)
                {
                    if (keptHashtags >= maxCount)
                        continue;
                    keptHashtags++;
                }

                rebuilt.Add(part);
            }

            output.Add(string.Join(' ', rebuilt));
        }

        while (output.Count > 0 && string.IsNullOrWhiteSpace(output[^1]))
            output.RemoveAt(output.Count - 1);

        return string.Join('\n', output).Trim();
    }

    static string FormatPriceLine(decimal price, decimal? salePrice, bool onSale, bool plainText)
    {
        var regular = price.ToString("N0", CultureInfo.InvariantCulture);
        if (!onSale || salePrice is null)
            return $"Price: Rs. {regular}";

        var sale = salePrice.Value.ToString("N0", CultureInfo.InvariantCulture);
        if (plainText)
            return $"Price: Rs. {sale} (was Rs. {regular})";

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

    static string FormatTags(string? tags, int? maxCount = null)
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

        if (maxCount is > 0 && parsed.Count > maxCount.Value)
            parsed = parsed.Take(maxCount.Value).ToList();

        return parsed.Count == 0 ? string.Empty : string.Join(' ', parsed);
    }
}
