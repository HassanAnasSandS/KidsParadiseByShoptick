namespace KidsParadiseByShoptick.Application.Helpers;

public static class SocialMediaTagHelper
{
    public static string FormatTagsForCaption(string? tags)
    {
        var parsed = ParseTags(tags);
        if (parsed.Count == 0)
            return string.Empty;

        return string.Join(' ', parsed.Select(EnsureHashPrefix));
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
