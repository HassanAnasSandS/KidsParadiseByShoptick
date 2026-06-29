namespace KidsParadiseByShoptick.Application.Helpers;

public static class CategorySort
{
    public static string SortKey(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var trimmed = name.Trim();
        var index = 0;
        while (index < trimmed.Length && !char.IsLetterOrDigit(trimmed[index]))
            index++;

        return index < trimmed.Length ? trimmed[index..].Trim() : trimmed;
    }

    public static List<T> ByName<T>(IEnumerable<T> items, Func<T, string> nameSelector) =>
        items.OrderBy(x => SortKey(nameSelector(x)), StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => nameSelector(x), StringComparer.OrdinalIgnoreCase)
            .ToList();
}
