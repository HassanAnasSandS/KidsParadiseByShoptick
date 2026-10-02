namespace KidsParadiseByShoptick.Application.Helpers;

public static class SocialMediaTagHelper
{
    /// <summary>TikTok allows only a small set of hashtags per post (keep ≤ 5).</summary>
    public const int TikTokMaxHashtags = 5;

    public static string FormatTagsForCaption(string? tags, int? maxCount = null)
    {
        var parsed = ParseTags(tags)
            .Select(EnsureHashPrefix)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (maxCount is > 0 && parsed.Count > maxCount.Value)
            parsed = parsed.Take(maxCount.Value).ToList();

        return parsed.Count == 0 ? string.Empty : string.Join(' ', parsed);
    }

    /// <summary>
    /// Keeps non-hashtag caption text and at most <paramref name="maxCount"/> hashtags
    /// (first occurrence order). Extra hashtags are dropped.
    /// </summary>
    public static string LimitHashtagsInCaption(string? caption, int maxCount)
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

        // Trim trailing empty lines created by removing a hashtag-only last line
        while (output.Count > 0 && string.IsNullOrWhiteSpace(output[^1]))
            output.RemoveAt(output.Count - 1);

        return string.Join('\n', output).Trim();
    }

    public static IReadOnlyList<string> ParseTagsForYouTube(string? tags)
    {
        return ParseTags(tags)
            .Select(StripHashPrefix)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return [];

        return tags
            .Split([',', '\n', '\r', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(t => t.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    private static string EnsureHashPrefix(string tag)
    {
        var trimmed = tag.Trim();
        return trimmed.StartsWith('#') ? trimmed : $"#{trimmed}";
    }

    private static string StripHashPrefix(string tag) => tag.Trim().TrimStart('#');
}
